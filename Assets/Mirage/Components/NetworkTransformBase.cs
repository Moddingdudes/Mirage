// vis2k:
// base class for NetworkTransform and NetworkTransformChild.
// New method is simple and stupid. No more 1500 lines of code.
//
// Server sends current data.
// Client saves it and interpolates last and latest data points.
//   Update handles transform movement / rotation
//   FixedUpdate handles rigidbody movement / rotation
//
// Notes:
// * Built-in Teleport detection in case of lags / teleport / obstacles
// * Quaternion > EulerAngles because gimbal lock and Quaternion.Slerp
// * Syncs XYZ. Works 3D and 2D. Saving 4 bytes isn't worth 1000 lines of code.
// * Initial delay might happen if server sends packet immediately after moving
//   just 1cm, hence we move 1cm and then wait 100ms for next packet
// * Only way for smooth movement is to use a fixed movement speed during
//   interpolation. interpolation over time is never that good.
//
using Mirage.Serialization;
using UnityEngine;

namespace Mirage
{
    public abstract class NetworkTransformBase : NetworkBehaviour
    {
        [Header("Authority")]
        [Tooltip("Set to true if moves come from owner client, set to false if moves always come from server")]
        public bool ClientAuthority;

        // Is this a client with authority over this transform?
        // This component could be on the player object or any object that has been assigned authority to this client.
        private bool IsClientWithAuthority => this.HasAuthority && this.ClientAuthority;

        // Sensitivity is added for VR where human players tend to have micro movements so this can quiet down
        // the network traffic.  Additionally, rigidbody drift should send less traffic, e.g very slow sliding / rolling.
        [Header("Sensitivity")]
        [Tooltip("Changes to the transform must exceed these values to be transmitted on the network.")]
        public float LocalPositionSensitivity = .01f;
        [Tooltip("If rotation exceeds this angle, it will be transmitted on the network")]
        public float LocalRotationSensitivity = .01f;
        [Tooltip("Changes to the transform must exceed these values to be transmitted on the network.")]
        public float LocalScaleSensitivity = .01f;

        // target transform to sync. can be on a child.
        protected abstract Transform TargetComponent { get; }

        // server
        private Vector3 lastPosition;
        private Quaternion lastRotation;
        private Vector3 lastScale;

        // client
        public class DataPoint
        {
            public float TimeStamp;
            // use local position/rotation for VR support
            public Vector3 LocalPosition;
            public Quaternion LocalRotation;
            public Vector3 LocalScale;
            public float MovementSpeed;
        }

        // interpolation start and goal
        private DataPoint start;
        private DataPoint goal;

        // local authority send time
        private float lastClientSendTime;

        // serialization is needed by OnSerialize and by manual sending from authority
        // public only for tests
        public static void SerializeIntoWriter(NetworkWriter writer, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            // serialize position, rotation, scale
            // note: we do NOT compress rotation.
            //       we are CPU constrained, not bandwidth constrained.
            //       the code needs to WORK for the next 5-10 years of development.
            writer.WriteVector3(position);
            writer.WriteQuaternion(rotation);
            writer.WriteVector3(scale);
        }

        public override bool OnSerialize(NetworkWriter writer, bool initialState)
        {
            // use local position/rotation/scale for VR support
            SerializeIntoWriter(writer, this.TargetComponent.localPosition, this.TargetComponent.localRotation, this.TargetComponent.localScale);
            return true;
        }

        // try to estimate movement speed for a data point based on how far it
        // moved since the previous one
        // => if this is the first time ever then we use our best guess:
        //    -> delta based on transform.localPosition
        //    -> elapsed based on send interval hoping that it roughly matches
        private static float EstimateMovementSpeed(DataPoint from, DataPoint to, Transform transform, float sendInterval)
        {
            var delta = to.LocalPosition - (from != null ? from.LocalPosition : transform.localPosition);
            var elapsed = from != null ? to.TimeStamp - from.TimeStamp : sendInterval;
            // avoid NaN
            return elapsed > 0 ? delta.magnitude / elapsed : 0;
        }

        // serialization is needed by OnSerialize and by manual sending from authority
        private void DeserializeFromReader(NetworkReader reader)
        {
            // put it into a data point immediately
            var temp = new DataPoint
            {
                // deserialize position
                LocalPosition = reader.ReadVector3()
            };

            // deserialize rotation & scale
            temp.LocalRotation = reader.ReadQuaternion();
            temp.LocalScale = reader.ReadVector3();

            temp.TimeStamp = Time.time;

            // movement speed: based on how far it moved since last time
            // has to be calculated before 'start' is overwritten
            temp.MovementSpeed = EstimateMovementSpeed(this.goal, temp, this.TargetComponent, this.syncInterval);

            // reassign start wisely
            // -> first ever data point? then make something up for previous one
            //    so that we can start interpolation without waiting for next.
            if (this.start == null)
            {
                this.start = new DataPoint
                {
                    TimeStamp = Time.time - this.syncInterval,
                    // local position/rotation for VR support
                    LocalPosition = this.TargetComponent.localPosition,
                    LocalRotation = this.TargetComponent.localRotation,
                    LocalScale = this.TargetComponent.localScale,
                    MovementSpeed = temp.MovementSpeed
                };
            }
            // -> second or nth data point? then update previous, but:
            //    we start at where ever we are right now, so that it's
            //    perfectly smooth and we don't jump anywhere
            //
            //    example if we are at 'x':
            //
            //        A--x->B
            //
            //    and then receive a new point C:
            //
            //        A--x--B
            //              |
            //              |
            //              C
            //
            //    then we don't want to just jump to B and start interpolation:
            //
            //              x
            //              |
            //              |
            //              C
            //
            //    we stay at 'x' and interpolate from there to C:
            //
            //           x..B
            //            \ .
            //             \.
            //              C
            //
            else
            {
                var oldDistance = Vector3.Distance(this.start.LocalPosition, this.goal.LocalPosition);
                var newDistance = Vector3.Distance(this.goal.LocalPosition, temp.LocalPosition);

                this.start = this.goal;

                // teleport / lag / obstacle detection: only continue at current
                // position if we aren't too far away
                //
                // local position/rotation for VR support
                //
                if (Vector3.Distance(this.TargetComponent.localPosition, this.start.LocalPosition) < oldDistance + newDistance)
                {
                    this.start.LocalPosition = this.TargetComponent.localPosition;
                    this.start.LocalRotation = this.TargetComponent.localRotation;
                    this.start.LocalScale = this.TargetComponent.localScale;
                }
            }

            // set new destination in any case. new data is best data.
            this.goal = temp;
        }

        public override void OnDeserialize(NetworkReader reader, bool initialState)
        {
            // deserialize
            this.DeserializeFromReader(reader);
        }

        // local authority client sends sync message to server for broadcasting
        [ServerRpc]
        private void CmdClientToServerSync(byte[] payload)
        {
            // Ignore messages from client if not in client authority mode
            if (!this.ClientAuthority)
                return;

            // deserialize payload
            using (var networkReader = NetworkReaderPool.GetReader(payload, this.Server.World))
                this.DeserializeFromReader(networkReader);

            // server-only mode does no interpolation to save computations,
            // but let's set the position directly
            if (this.IsServer && !this.IsClient)
                this.ApplyPositionRotationScale(this.goal.LocalPosition, this.goal.LocalRotation, this.goal.LocalScale);

            // set dirty so that OnSerialize broadcasts it
            this.SetDirtyBit(1UL);
        }

        // where are we in the timeline between start and goal? [0,1]
        private static float CurrentInterpolationFactor(DataPoint start, DataPoint goal)
        {
            if (start != null)
            {
                var difference = goal.TimeStamp - start.TimeStamp;

                // the moment we get 'goal', 'start' is supposed to
                // start, so elapsed time is based on:
                var elapsed = Time.time - goal.TimeStamp;
                // avoid NaN
                return difference > 0 ? elapsed / difference : 0;
            }
            return 0;
        }

        private static Vector3 InterpolatePosition(DataPoint start, DataPoint goal, Vector3 currentPosition)
        {
            if (start != null)
            {
                // Option 1: simply interpolate based on time. but stutter
                // will happen, it's not that smooth. especially noticeable if
                // the camera automatically follows the player
                //   float t = CurrentInterpolationFactor();
                //   return Vector3.Lerp(start.position, goal.position, t);

                // Option 2: always += speed
                // -> speed is 0 if we just started after idle, so always use max
                //    for best results
                var speed = Mathf.Max(start.MovementSpeed, goal.MovementSpeed);
                return Vector3.MoveTowards(currentPosition, goal.LocalPosition, speed * Time.deltaTime);
            }
            return currentPosition;
        }

        private static Quaternion InterpolateRotation(DataPoint start, DataPoint goal, Quaternion defaultRotation)
        {
            if (start != null)
            {
                var t = CurrentInterpolationFactor(start, goal);
                return Quaternion.Slerp(start.LocalRotation, goal.LocalRotation, t);
            }
            return defaultRotation;
        }

        private static Vector3 InterpolateScale(DataPoint start, DataPoint goal, Vector3 currentScale)
        {
            if (start != null)
            {
                var t = CurrentInterpolationFactor(start, goal);
                return Vector3.Lerp(start.LocalScale, goal.LocalScale, t);
            }
            return currentScale;
        }

        // teleport / lag / stuck detection
        // -> checking distance is not enough since there could be just a tiny
        //    fence between us and the goal
        // -> checking time always works, this way we just teleport if we still
        //    didn't reach the goal after too much time has elapsed
        private bool NeedsTeleport()
        {
            // calculate time between the two data points
            var startTime = this.start != null ? this.start.TimeStamp : Time.time - this.syncInterval;
            var goalTime = this.goal != null ? this.goal.TimeStamp : Time.time;
            var difference = goalTime - startTime;
            var timeSinceGoalReceived = Time.time - goalTime;
            return timeSinceGoalReceived > difference * 5;
        }

        // moved since last time we checked it?
        private bool HasEitherMovedRotatedScaled()
        {
            // moved or rotated or scaled?
            // local position/rotation/scale for VR support
            var moved = Vector3.Distance(this.lastPosition, this.TargetComponent.localPosition) > this.LocalPositionSensitivity;
            var scaled = Vector3.Distance(this.lastScale, this.TargetComponent.localScale) > this.LocalScaleSensitivity;
            var rotated = Quaternion.Angle(this.lastRotation, this.TargetComponent.localRotation) > this.LocalRotationSensitivity;

            // save last for next frame to compare
            // (only if change was detected. otherwise slow moving objects might
            //  never sync because of C#'s float comparison tolerance. see also:
            //  https://github.com/vis2k/Mirror/pull/428)
            var change = moved || rotated || scaled;
            if (change)
            {
                // local position/rotation for VR support
                this.lastPosition = this.TargetComponent.localPosition;
                this.lastRotation = this.TargetComponent.localRotation;
                this.lastScale = this.TargetComponent.localScale;
            }
            return change;
        }

        // set position carefully depending on the target component
        private void ApplyPositionRotationScale(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            // local position/rotation for VR support
            this.TargetComponent.localPosition = position;
            this.TargetComponent.localRotation = rotation;
            this.TargetComponent.localScale = scale;
        }

        private void Update()
        {
            // if server then always sync to others.
            if (this.IsServer)
            {
                this.UpdateServer();
            }

            // no 'else if' since host mode would be both
            if (this.IsClient)
            {
                this.UpdateClient();
            }
        }

        private void UpdateServer()
        {
            // just use OnSerialize via SetDirtyBit only sync when position
            // changed. set dirty bits 0 or 1
            this.SetDirtyBit(this.HasEitherMovedRotatedScaled() ? 1UL : 0UL);
        }

        private void UpdateClient()
        {
            // send to server if we have local authority (and aren't the server)
            // -> only if connectionToServer has been initialized yet too
            // check only each 'syncInterval'
            if (!this.IsServer && this.IsClientWithAuthority && Time.time - this.lastClientSendTime >= this.syncInterval)
            {
                if (this.HasEitherMovedRotatedScaled())
                {
                    // serialize
                    // local position/rotation for VR support
                    using (var writer = NetworkWriterPool.GetWriter())
                    {
                        SerializeIntoWriter(writer, this.TargetComponent.localPosition, this.TargetComponent.localRotation, this.TargetComponent.localScale);

                        // send to server
                        this.CmdClientToServerSync(writer.ToArray());
                    }
                }
                this.lastClientSendTime = Time.time;
            }

            // apply interpolation on client for all players
            // unless this client has authority over the object. could be
            // himself or another object that he was assigned authority over
            if (!this.IsClientWithAuthority && this.goal != null)
            {
                // teleport or interpolate
                if (this.NeedsTeleport())
                {
                    // local position/rotation for VR support
                    this.ApplyPositionRotationScale(this.goal.LocalPosition, this.goal.LocalRotation, this.goal.LocalScale);

                    // reset data points so we don't keep interpolating
                    this.start = null;
                    this.goal = null;
                }
                else
                {
                    // local position/rotation for VR support
                    this.ApplyPositionRotationScale(InterpolatePosition(this.start, this.goal, this.TargetComponent.localPosition),
                                                InterpolateRotation(this.start, this.goal, this.TargetComponent.localRotation),
                                                InterpolateScale(this.start, this.goal, this.TargetComponent.localScale));
                }
            }
        }

        private static void DrawDataPointGizmo(DataPoint data, Color color)
        {
            // use a little offset because transform.localPosition might be in
            // the ground in many cases
            var offset = Vector3.up * 0.01f;

            // draw position
            Gizmos.color = color;
            Gizmos.DrawSphere(data.LocalPosition + offset, 0.5f);

            // draw forward and up
            // like unity move tool
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(data.LocalPosition + offset, data.LocalRotation * Vector3.forward);

            // like unity move tool
            Gizmos.color = Color.green;
            Gizmos.DrawRay(data.LocalPosition + offset, data.LocalRotation * Vector3.up);
        }

        private static void DrawLineBetweenDataPoints(DataPoint data1, DataPoint data2, Color color)
        {
            Gizmos.color = color;
            Gizmos.DrawLine(data1.LocalPosition, data2.LocalPosition);
        }

        // draw the data points for easier debugging
        private void OnDrawGizmos()
        {
            // draw start and goal points
            if (this.start != null) DrawDataPointGizmo(this.start, Color.gray);
            if (this.goal != null) DrawDataPointGizmo(this.goal, Color.white);

            // draw line between them
            if (this.start != null && this.goal != null) DrawLineBetweenDataPoints(this.start, this.goal, Color.cyan);
        }
    }
}
