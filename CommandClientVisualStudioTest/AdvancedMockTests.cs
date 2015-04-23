using System;
using System.Net;
using System.Reflection;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Proshot.CommandClient;
using System.Collections.Generic;
using Rhino.Mocks;
using System.Linq;

namespace CommandClientVisualStudioTest
{
    [TestClass]
    public class AdvancedMockTests
    {
        private MockRepository mocks;

        [TestMethod]
        public void VerySimpleTest()
        {
            CMDClient client = new CMDClient(null, "Bogus network name");
            Assert.AreEqual("Bogus network name", client.NetworkName);
        }

        [TestInitialize()]
        public void Initialize()
        {
            mocks = new MockRepository();
        }

        [TestMethod]
        public void TestUserExitCommand()
        {
            IPAddress ipaddress = IPAddress.Parse("127.0.0.1");
            Command command = new Command(CommandType.UserExit, ipaddress, null);
            System.IO.Stream fakeStream = mocks.DynamicMock<System.IO.Stream>();

            byte[] commandBytes = { 0, 0, 0, 0 };
            byte[] ipLength = { 9, 0, 0, 0 };
            byte[] ip = { 49, 50, 55, 46, 48, 46, 48, 46, 49 };
            byte[] metaDataLength = { 2, 0, 0, 0 };
            byte[] metaData = { 10, 0 };

            using (mocks.Ordered())
            {
                fakeStream.Write(commandBytes, 0, 4);
                fakeStream.Flush();
                fakeStream.Write(ipLength, 0, 4);
                fakeStream.Flush();
                fakeStream.Write(ip, 0, 9);
                fakeStream.Flush();
                fakeStream.Write(metaDataLength, 0, 4);
                fakeStream.Flush();
                fakeStream.Write(metaData, 0, 2);
                fakeStream.Flush();
            }
            mocks.ReplayAll();
            CMDClient client = new CMDClient(null, "Bogus network name");
            
            // we need to set the private variable here
            typeof(CMDClient).GetField("networkStream", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(client, fakeStream);
            client.SendCommandToServerUnthreaded(command);
            mocks.VerifyAll();
            
        }

        [TestMethod]
        public void TestUserExitCommandWithoutMocks()
        {
            IPAddress ipaddress = IPAddress.Parse("127.0.0.1");
            Command command = new Command(CommandType.UserExit, ipaddress, null);
            MemoryStream memStream = new MemoryStream();
            CMDClient client = new CMDClient(null, "Bogus network name");
            // we need to set the private variable here
            typeof(CMDClient).GetField("networkStream", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(client, memStream);
            client.SendCommandToServerUnthreaded(command);
        }

        [TestMethod]
        public void TestSemaphoreReleaseOnNormalOperation()
        {
            IPAddress ipaddress = IPAddress.Parse("127.0.0.1");
            Command command = new Command(CommandType.UserExit, ipaddress, null);
            System.IO.Stream fakeStream = mocks.DynamicMock<System.IO.Stream>();
            System.Threading.Semaphore fakeSema = mocks.DynamicMock<System.Threading.Semaphore>();

            byte[] commandBytes = { 0, 0, 0, 0 };
            byte[] ipLength = { 9, 0, 0, 0 };
            byte[] ip = { 49, 50, 55, 46, 48, 46, 48, 46, 49 };
            byte[] metaDataLength = { 2, 0, 0, 0 };
            byte[] metaData = { 10, 0 };

            using (mocks.Ordered())
            {
                Expect.Call(fakeSema.WaitOne()).Return(true);
                fakeStream.Write(commandBytes, 0, 4);
                fakeStream.Flush();
                fakeStream.Write(ipLength, 0, 4);
                fakeStream.Flush();
                fakeStream.Write(ip, 0, 9);
                fakeStream.Flush();
                fakeStream.Write(metaDataLength, 0, 4);
                fakeStream.Flush();
                fakeStream.Write(metaData, 0, 2);
                fakeStream.Flush();
                Expect.Call(fakeSema.Release()).Return(1);
            }
            mocks.ReplayAll();
            CMDClient client = new CMDClient(null, "Bogus network name");

            // we need to set the private variable here
            typeof(CMDClient).GetField("networkStream", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(client, fakeStream);
            typeof(CMDClient).GetField("semaphore", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(client, fakeSema);
            client.SendCommandToServerUnthreaded(command);
            mocks.VerifyAll();
        }

        [TestMethod]
        public void TestSemaphoreReleaseOnExceptionalOperation()
        {
            IPAddress ipaddress = IPAddress.Parse("127.0.0.1");
            Command command = new Command(CommandType.UserExit, ipaddress, null);
            System.IO.Stream fakeStream = mocks.DynamicMock<System.IO.Stream>();
            System.Threading.Semaphore fakeSema = mocks.DynamicMock<System.Threading.Semaphore>();

            byte[] commandBytes = { 0, 0, 0, 0 };
            byte[] ipLength = { 9, 0, 0, 0 };
            byte[] ip = { 49, 50, 55, 46, 48, 46, 48, 46, 49 };
            byte[] metaDataLength = { 2, 0, 0, 0 };
            byte[] metaData = { 10, 0 };

            using (mocks.Ordered())
            {
                Expect.Call(fakeSema.WaitOne()).Return(true);
                fakeStream.Write(commandBytes, 0, 4);
                fakeStream.Flush();
                fakeStream.Write(ipLength, 0, 4);
                fakeStream.Flush();
                fakeStream.Write(ip, 0, 9);
                fakeStream.Flush();
                fakeStream.Write(metaDataLength, 0, 4);
                fakeStream.Flush();
                fakeStream.Write(metaData, 0, 2);
                try
                {
                    fakeStream.Flush();
                    LastCall.On(fakeStream).Throw(new Exception("Error"));
                }
                catch(Exception)
                {
                    Expect.Call(fakeSema.Release()).Return(1);
                }
            }
            mocks.ReplayAll();
            CMDClient client = new CMDClient(null, "Bogus network name");

            // we need to set the private variable here
            typeof(CMDClient).GetField("networkStream", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(client, fakeStream);
            typeof(CMDClient).GetField("semaphore", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(client, fakeSema);
            try
            {
                client.SendCommandToServerUnthreaded(command);
            }
            catch(Exception)
            {
                mocks.VerifyAll();
            }
        }
    }
}
