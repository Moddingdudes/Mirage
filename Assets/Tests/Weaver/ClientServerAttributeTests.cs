using System;
using System.Linq;
using System.Linq.Expressions;
using Mirage.Weaver;
using Mono.Cecil.Cil;
using NUnit.Framework;

namespace Mirage.Tests.Weaver
{
    public class ClientServerAttributeTests : TestsBuildFromTestName
    {
        [Test]
        public void NetworkBehaviourServer()
        {
            this.IsSuccess();
            this.CheckAddedCode(
                (NetworkBehaviour nb) => nb.IsServer,
                "ClientServerAttributeTests.NetworkBehaviourServer.NetworkBehaviourServer", "ServerOnlyMethod");

        }

        [Test]
        public void NetworkBehaviourServerOnAwake()
        {
            this.HasError("ServerAttribute will not work on the Awake method.",
                "System.Void ClientServerAttributeTests.NetworkBehaviourServer.NetworkBehaviourServerOnAwake::Awake()");
        }

        [Test]
        public void NetworkBehaviourServerOnAwakeWithParameters()
        {
            this.IsSuccess();
            this.CheckAddedCode(
                (NetworkBehaviour nb) => nb.IsServer,
                "ClientServerAttributeTests.NetworkBehaviourServer.NetworkBehaviourServerOnAwakeWithParameters", "Awake");

        }

        [Test]
        public void NetworkBehaviourClient()
        {
            this.IsSuccess();
            this.CheckAddedCode(
                (NetworkBehaviour nb) => nb.IsClient,
                "ClientServerAttributeTests.NetworkBehaviourClient.NetworkBehaviourClient", "ClientOnlyMethod");
        }

        [Test]
        public void NetworkBehaviourClientOnAwake()
        {
            this.HasError("ClientAttribute will not work on the Awake method.",
                "System.Void ClientServerAttributeTests.NetworkBehaviourClient.NetworkBehaviourClientOnAwake::Awake()");
        }

        [Test]
        public void NetworkBehaviourClientOnAwakeWithParameters()
        {
            this.IsSuccess();
            this.CheckAddedCode(
                (NetworkBehaviour nb) => nb.IsClient,
                "ClientServerAttributeTests.NetworkBehaviourClient.NetworkBehaviourClientOnAwakeWithParameters", "Awake");
        }

        [Test]
        public void NetworkBehaviourHasAuthority()
        {
            this.IsSuccess();
            this.CheckAddedCode(
                (NetworkBehaviour nb) => nb.HasAuthority,
                "ClientServerAttributeTests.NetworkBehaviourHasAuthority.NetworkBehaviourHasAuthority", "HasAuthorityMethod");
        }

        [Test]
        public void NetworkBehaviourHasAuthorityOnAwake()
        {
            this.HasError("HasAuthorityAttribute will not work on the Awake method.",
                "System.Void ClientServerAttributeTests.NetworkBehaviourHasAuthority.NetworkBehaviourHasAuthorityOnAwake::Awake()");
        }

        [Test]
        public void NetworkBehaviourHasAuthorityOnAwakeWithParameters()
        {
            this.IsSuccess();
            this.CheckAddedCode(
                (NetworkBehaviour nb) => nb.HasAuthority,
                "ClientServerAttributeTests.NetworkBehaviourHasAuthority.NetworkBehaviourHasAuthorityOnAwakeWithParameters", "Awake");
        }

        [Test]
        public void NetworkBehaviourLocalPlayer()
        {
            this.IsSuccess();
            this.CheckAddedCode(
                (NetworkBehaviour nb) => nb.IsLocalPlayer,
                "ClientServerAttributeTests.NetworkBehaviourLocalPlayer.NetworkBehaviourLocalPlayer", "LocalPlayerMethod");
        }

        [Test]
        public void NetworkBehaviourLocalPlayerOnAwake()
        {
            this.HasError("LocalPlayerAttribute will not work on the Awake method.",
                "System.Void ClientServerAttributeTests.NetworkBehaviourLocalPlayer.NetworkBehaviourLocalPlayerOnAwake::Awake()");
        }

        [Test]
        public void NetworkBehaviourLocalPlayerOnAwakeWithParameters()
        {
            this.IsSuccess();
            this.CheckAddedCode(
                (NetworkBehaviour nb) => nb.IsLocalPlayer,
                "ClientServerAttributeTests.NetworkBehaviourLocalPlayer.NetworkBehaviourLocalPlayerOnAwakeWithParameters", "Awake");
        }

        /// <summary>
        /// Checks that first Instructions in MethodBody is addedString
        /// </summary>
        /// <param name="addedString"></param>
        /// <param name="methodName"></param>
        private void CheckAddedCode(Expression<Func<NetworkBehaviour, bool>> pred, string className, string methodName)
        {
            var type = this.assembly.MainModule.GetType(className);
            var method = type.Methods.First(m => m.Name == methodName);
            var body = method.Body;

            var top = body.Instructions[0];
            Assert.That(top.OpCode, Is.EqualTo(OpCodes.Ldarg_0));

            var methodRef = this.assembly.MainModule.ImportReference(pred);

            var call = body.Instructions[1];

            Assert.That(call.OpCode, Is.EqualTo(OpCodes.Call));
            Assert.That(call.Operand.ToString(), Is.EqualTo(methodRef.ToString()));
        }
    }
}
