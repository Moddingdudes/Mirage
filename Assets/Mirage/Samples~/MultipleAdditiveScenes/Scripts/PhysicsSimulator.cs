using UnityEngine;

namespace Mirage.Examples.MultipleAdditiveScenes
{
    public class PhysicsSimulator : NetworkBehaviour
    {
        private PhysicsScene physicsScene;
        private PhysicsScene2D physicsScene2D;
        private bool simulatePhysicsScene;
        private bool simulatePhysicsScene2D;

        private void Start()
        {
            if (this.IsServer)
            {
                this.physicsScene = this.gameObject.scene.GetPhysicsScene();
                this.simulatePhysicsScene = this.physicsScene.IsValid() && this.physicsScene != Physics.defaultPhysicsScene;

                this.physicsScene2D = this.gameObject.scene.GetPhysicsScene2D();
                this.simulatePhysicsScene2D = this.physicsScene2D.IsValid() && this.physicsScene2D != Physics2D.defaultPhysicsScene;
            }
            else
            {
                this.enabled = false;
            }
        }

        private void FixedUpdate()
        {
            if (!this.IsServer) return;

            if (this.simulatePhysicsScene)
                this.physicsScene.Simulate(Time.fixedDeltaTime);

            if (this.simulatePhysicsScene2D)
                this.physicsScene2D.Simulate(Time.fixedDeltaTime);
        }
    }
}
