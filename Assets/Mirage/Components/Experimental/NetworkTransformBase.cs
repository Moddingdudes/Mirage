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
using UnityEngine;

namespace Mirage.Experimental
{
    public abstract class NetworkTransformBase : NetworkBehaviour
    {
        // target transform to sync. can be on a child.
        protected abstract Transform TargetTransform { get; }

        [Header("Authority")]

        [Tooltip("Set to true if moves come from owner client, set to false if moves always come from server")]
        [SyncVar]
        public bool clientAuthority;

        [Tooltip("Set to true if updates from server should be ignored by owner")]
        [SyncVar]
        public bool excludeOwnerUpdate = true;

        [Header("Synchronization")]

        [Tooltip("Set to true if position should be synchronized")]
        [SyncVar]
        public bool syncPosition = true;

        [Tooltip("Set to true if rotation should be synchronized")]
        [SyncVar]
        public bool syncRotation = true;

        [Tooltip("Set to true if scale should be synchronized")]
        [SyncVar]
        public bool syncScale = true;

        [Header("Interpolation")]

        [Tooltip("Set to true if position should be interpolated")]
        [SyncVar]
        public bool interpolatePosition = true;

        [Tooltip("Set to true if rotation should be interpolated")]
        [SyncVar]
        public bool interpolateRotation = true;

        [Tooltip("Set to true if scale should be interpolated")]
        [SyncVar]
        public bool interpolateScale = true;

        // Sensitivity is added for VR where human players tend to have micro movements so this can quiet down
        // the network traffic.  Additionally, rigidbody drift should send less traffic, e.g very slow sliding / rolling.
        [Header("Sensitivity")]

        [Tooltip("Changes to the transform must exceed these values to be transmitted on the network.")]
        [SyncVar]
        public float localPositionSensitivity = .01f;

        [Tooltip("If rotation exceeds this angle, it will be transmitted on the network")]
        [SyncVar]
        public float localRotationSensitivity = .01f;

        [Tooltip("Changes to the transform must exceed these values to be transmitted on the network.")]
        [SyncVar]
        public float localScaleSensitivity = .01f;

        [Header("Diagnostics")]

        // server
        public Vector3 lastPosition;
        public Quaternion lastRotation;
        public Vector3 lastScale;

        // client
        // use local position/rotation for VR support
        [System.Serializable]
        public struct DataPoint
        {
            public float timeStamp;
            public Vector3 localPosition;
            public Quaternion localRotation;
            public Vector3 localScale;
            public float movementSpeed;

            public bool IsValid => this.timeStamp != 0;
        }

        // Is this a client with authority over this transform?
        // This component could be on the player object or any object that has been assigned authority to this client.
        private bool IsOwnerWithClientAuthority => this.HasAuthority && this.clientAuthority;

        // interpolation start and goal
        public DataPoint start = new DataPoint();
        public DataPoint goal = new DataPoint();

        private void FixedUpdate()
        {
            // if server then always sync to others.
            // let the clients know that this has moved
            if (this.IsServer && this.HasEitherMovedRotatedScaled())
            {
                this.RpcMove(this.TargetTransform.localPosition, this.TargetTransform.localRotation, this.TargetTransform.localScale);
            }

            if (this.IsClient)
            {
                // send to server if we have local authority (and aren't the server)
                // -> only if connectionToServer has been initialized yet too
                if (this.IsOwnerWithClientAuthority)
                {
                    if (!this.IsServer && this.HasEitherMovedRotatedScaled())
                    {
                        // serialize
                        // local position/rotation for VR support
                        // send to server
                        this.CmdClientToServerSync(this.TargetTransform.localPosition, this.TargetTransform.localRotation, this.TargetTransform.localScale);
                    }
                }
                else if (this.goal.IsValid)
                {
                    // teleport or interpolate
                    if (this.NeedsTeleport())
                    {
                        // local position/rotation for VR support
                        this.ApplyPositionRotationScale(this.goal.localPosition, this.goal.localRotation, this.goal.localScale);

                        // reset data points so we don't keep interpolating
                        this.start = new DataPoint();
                        this.goal = new DataPoint();
                    }
                    else
                    {
                        // local position/rotation for VR support
                        this.ApplyPositionRotationScale(this.InterpolatePosition(this.start, this.goal, this.TargetTransform.localPosition),
                                                   this.InterpolateRotation(this.start, this.goal, this.TargetTransform.localRotation),
                                                   this.InterpolateScale(this.start, this.goal, this.TargetTransform.localScale));
                    }
                }
            }
        }

        // moved or rotated or scaled since last time we checked it?
        private bool HasEitherMovedRotatedScaled()
        {
            // Save last for next frame to compare only if change was detected, otherwise
            // slow moving objects might never sync because of C#'s float comparison tolerance.
            // See also: https://github.com/vis2k/Mirror/pull/428)
            var changed = this.HasMoved || this.HasRotated || this.HasScaled;
            if (changed)
            {
                // local position/rotation for VR support
                if (this.syncPosition) this.lastPosition = this.TargetTransform.localPosition;
                if (this.syncRotation) this.lastRotation = this.TargetTransform.localRotation;
                if (this.syncScale) this.lastScale = this.TargetTransform.localScale;
            }
            return changed;
        }

        // local position/rotation for VR support
        // SqrMagnitude is faster than Distance per Unity docs
        // https://docs.unity3d.com/ScriptReference/Vector3-sqrMagnitude.html

        private bool HasMoved => this.syncPosition && Vector3.SqrMagnitude(this.lastPosition - this.TargetTransform.localPosition) > this.localPositionSensitivity * this.localPositionSensitivity;

        private bool HasRotated => this.syncRotation && Quaternion.Angle(this.lastRotation, this.TargetTransform.localRotation) > this.localRotationSensitivity;

        private bool HasScaled => this.syncScale && Vector3.SqrMagnitude(this.lastScale - this.TargetTransform.localScale) > this.localScaleSensitivity * this.localScaleSensitivity;

        // teleport / lag / stuck detection
        // - checking distance is not enough since there could be just a tiny fence between us and the goal
        // - checking time always works, this way we just teleport if we still didn't reach the goal after too much time has elapsed
        private bool NeedsTeleport()
        {
            // calculate time between the two data points
            var startTime = this.start.IsValid ? this.start.timeStamp : Time.time - Time.fixedDeltaTime;
            var goalTime = this.goal.IsValid ? this.goal.timeStamp : Time.time;
            var difference = goalTime - startTime;
            var timeSinceGoalReceived = Time.time - goalTime;
            return timeSinceGoalReceived > difference * 5;
        }

        // local authority client sends sync message to server for broadcasting
        [ServerRpc]
        private void CmdClientToServerSync(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            // Ignore messages from client if not in client authority mode
            if (!this.clientAuthority)
                return;

            // deserialize payload
            this.SetGoal(position, rotation, scale);

            // server-only mode does no interpolation to save computations, but let's set the position directly
            if (this.IsServer && !this.IsClient)
                this.ApplyPositionRotationScale(this.goal.localPosition, this.goal.localRotation, this.goal.localScale);

            this.RpcMove(position, rotation, scale);
        }

        [ClientRpc]
        private void RpcMove(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            if (this.HasAuthority && this.excludeOwnerUpdate) return;

            if (!this.IsServer)
                this.SetGoal(position, rotation, scale);
        }

        // serialization is needed by OnSerialize and by manual sending from authority
        private void SetGoal(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            // put it into a data point immediately
            var temp = new DataPoint
            {
                // deserialize position
                localPosition = position,
                localRotation = rotation,
                localScale = scale,
                timeStamp = Time.time
            };

            // movement speed: based on how far it moved since last time has to be calculated before 'start' is overwritten
            temp.movementSpeed = EstimateMovementSpeed(this.goal, temp, this.TargetTransform, Time.fixedDeltaTime);

            // reassign start wisely
            // first ever data point? then make something up for previous one so that we can start interpolation without waiting for next.
            if (this.start.timeStamp == 0)
            {
                this.start = new DataPoint
                {
                    timeStamp = Time.time - Time.fixedDeltaTime,
                    // local position/rotation for VR support
                    localPosition = this.TargetTransform.localPosition,
                    localRotation = this.TargetTransform.localRotation,
                    localScale = this.TargetTransform.localScale,
                    movementSpeed = temp.movementSpeed
                };
            }
            // second or nth data point? then update previous
            // but: we start at where ever we are right now, so that it's perfectly smooth and we don't jump anywhere
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
                var oldDistance = Vector3.Distance(this.start.localPosition, this.goal.localPosition);
                var newDistance = Vector3.Distance(this.goal.localPosition, temp.localPosition);

                this.start = this.goal;

                // local position/rotation for VR support
                // teleport / lag / obstacle detection: only continue at current position if we aren't too far away
                // XC  < AB + BC (see comments above)
                if (Vector3.Distance(this.TargetTransform.localPosition, this.start.localPosition) < oldDistance + newDistance)
                {
                    this.start.localPosition = this.TargetTransform.localPosition;
                    this.start.localRotation = this.TargetTransform.localRotation;
                    this.start.localScale = this.TargetTransform.localScale;
                }
            }

            // set new destination in any case. new data is best data.
            this.goal = temp;
        }

        // try to estimate movement speed for a data point based on how far it moved since the previous one
        // - if this is the first time ever then we use our best guess:
        //     - delta based on transform.localPosition
        //     - elapsed based on send interval hoping that it roughly matches
        private static float EstimateMovementSpeed(DataPoint from, DataPoint to, Transform transform, float sendInterval)
        {
            var delta = to.localPosition - (from.localPosition != transform.localPosition ? from.localPosition : transform.localPosition);
            var elapsed = from.IsValid ? to.timeStamp - from.timeStamp : sendInterval;

            // avoid NaN
            return elapsed > 0 ? delta.magnitude / elapsed : 0;
        }

        // set position carefully depending on the target component
        private void ApplyPositionRotationScale(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            // local position/rotation for VR support
            if (this.syncPosition) this.TargetTransform.localPosition = position;
            if (this.syncRotation) this.TargetTransform.localRotation = rotation;
            if (this.syncScale) this.TargetTransform.localScale = scale;
        }

        // where are we in the timeline between start and goal? [0,1]
        private Vector3 InterpolatePosition(DataPoint start, DataPoint goal, Vector3 currentPosition)
        {
            if (!this.interpolatePosition)
                return currentPosition;

            if (start.movementSpeed != 0)
            {
                // Option 1: simply interpolate based on time, but stutter will happen, it's not that smooth.
                // This is especially noticeable if the camera automatically follows the player
                // -         Tell SonarCloud this isn't really commented code but actual comments and to stfu about it
                // -         float t = CurrentInterpolationFactor();
                // -         return Vector3.Lerp(start.position, goal.position, t);

                // Option 2: always += speed
                // speed is 0 if we just started after idle, so always use max for best results
                var speed = Mathf.Max(start.movementSpeed, goal.movementSpeed);
                return Vector3.MoveTowards(currentPosition, goal.localPosition, speed * Time.deltaTime);
            }

            return currentPosition;
        }

        private Quaternion InterpolateRotation(DataPoint start, DataPoint goal, Quaternion defaultRotation)
        {
            if (!this.interpolateRotation)
                return defaultRotation;

            if (start.localRotation != goal.localRotation)
            {
                var t = CurrentInterpolationFactor(start, goal);
                return Quaternion.Slerp(start.localRotation, goal.localRotation, t);
            }

            return defaultRotation;
        }

        private Vector3 InterpolateScale(DataPoint start, DataPoint goal, Vector3 currentScale)
        {
            if (!this.interpolateScale)
                return currentScale;

            if (start.localScale != goal.localScale)
            {
                var t = CurrentInterpolationFactor(start, goal);
                return Vector3.Lerp(start.localScale, goal.localScale, t);
            }

            return currentScale;
        }

        private static float CurrentInterpolationFactor(DataPoint start, DataPoint goal)
        {
            if (start.IsValid)
            {
                var difference = goal.timeStamp - start.timeStamp;

                // the moment we get 'goal', 'start' is supposed to start, so elapsed time is based on:
                var elapsed = Time.time - goal.timeStamp;

                // avoid NaN
                return difference > 0 ? elapsed / difference : 1;
            }
            return 1;
        }

        #region Debug Gizmos

        // draw the data points for easier debugging
        private void OnDrawGizmos()
        {
            // draw start and goal points and a line between them
            if (this.start.localPosition != this.goal.localPosition)
            {
                DrawDataPointGizmo(this.start, Color.yellow);
                DrawDataPointGizmo(this.goal, Color.green);
                DrawLineBetweenDataPoints(this.start, this.goal, Color.cyan);
            }
        }

        private static void DrawDataPointGizmo(DataPoint data, Color color)
        {
            // use a little offset because transform.localPosition might be in the ground in many cases
            var offset = Vector3.up * 0.01f;

            // draw position
            Gizmos.color = color;
            Gizmos.DrawSphere(data.localPosition + offset, 0.5f);

            // draw forward and up like unity move tool
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(data.localPosition + offset, data.localRotation * Vector3.forward);
            Gizmos.color = Color.green;
            Gizmos.DrawRay(data.localPosition + offset, data.localRotation * Vector3.up);
        }

        private static void DrawLineBetweenDataPoints(DataPoint data1, DataPoint data2, Color color)
        {
            Gizmos.color = color;
            Gizmos.DrawLine(data1.localPosition, data2.localPosition);
        }

        #endregion
    }
}
