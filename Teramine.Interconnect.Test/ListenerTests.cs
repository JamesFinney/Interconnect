#region License
// Copyright (c) 2019 Teramine Ltd
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
#endregion

using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Teramine.DSC.Interconnect.Exceptions;
using Teramine.DSC.Interconnect.Interfaces;

namespace Teramine.DSC.Interconnect.Test
{
    [TestFixture]
    public class ListenerTests
    {
        public Mock<IComponentFactory> MockComponentFactory;


        [OneTimeSetUp]
        public void Setup()
        {
            MockComponentFactory = new Mock<IComponentFactory>();
            Listener.ComponentFactory = MockComponentFactory.Object;
        }

        #region Constructor

        [Test]
        public void Constructor_InvalidHost()
        {
            Assert.Throws<ArgumentNullException>(() => new Listener(null, 35000, null));
            Assert.Throws<ConnectionException>(() => new Listener("$%&*^()FESF$£124", 35000, null));
            Assert.Throws<ConnectionException>(() => new Listener("1024.1024.1024.1024", 35000, null));
        }

        [Test]
        public void Constructor_InvalidPort()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Listener("localhost", 0, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Listener("localhost", 100000, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Listener("localhost", -10, null));
        }

        [Test]
        public void Constructor_ValidParameters()
        {
            Assert.DoesNotThrow(() => { _ = new Listener("127.0.0.1", 1, null); });
            Assert.DoesNotThrow(() => { _ = new Listener("localhost", 65535, null); });
            Assert.DoesNotThrow(() => { _ = new Listener(Environment.GetEnvironmentVariable("COMPUTERNAME"), 35000, null); });
        }

        #endregion

        #region Start

        [Test]
        public void Start_TcpListenerSetupAndStarted()
        {
            #region Basic factory setup

            var mockTcpListener = new Mock<ITcpListener>();
            var mockTcpClient = new Mock<ITcpClient>();

            MockComponentFactory.Setup(x => x.BuildTcpListener(It.IsAny<IPAddress>(), It.IsAny<int>(),  null))
                .Returns(mockTcpListener.Object);
            MockComponentFactory.Setup(x => x.BuildTcpClient(It.IsAny<string>(), It.IsAny<int>(), null))
                .Returns(mockTcpClient.Object);

            #endregion

            var listener = new Listener("127.0.0.1", 35000, null);        // note: 'localhost' setups up 2 listeners; 0.0.0.0 and "::"
            var returnedListener = listener.Start();

            MockComponentFactory.Verify(x => x.BuildTcpListener(It.IsAny<IPAddress>(), It.IsAny<int>(), null), Times.AtLeastOnce);
            mockTcpListener.Verify(x => x.Start(), Times.Once);
            mockTcpListener.Verify(x => x.BeginAcceptTcpClient(It.IsAny<AsyncCallback>(), It.IsAny<object>()), Times.Once);
            Assert.AreEqual(listener, returnedListener);
        }

        [Test]
        public void Start_ListenerThrowsException()
        {
            #region Basic factory setup

            var mockTcpListener = new Mock<ITcpListener>();
            var mockTcpClient = new Mock<ITcpClient>();

            MockComponentFactory.Setup(x => x.BuildTcpListener(It.IsAny<IPAddress>(), It.IsAny<int>(), null))
                .Returns(mockTcpListener.Object);
            MockComponentFactory.Setup(x => x.BuildTcpClient(It.IsAny<string>(), It.IsAny<int>(), null))
                .Returns(mockTcpClient.Object);

            #endregion

            mockTcpListener.Setup(x => x.Start()).Throws(new Exception());

            var listener = new Listener("127.0.0.1", 35000, null);

            Assert.Throws<ConnectionException>(() => listener.Start() );
        }

        #endregion

        #region Stop

        [Test]
        public void Stop_ListenersStopped()
        {
            #region Basic factory setup

            var mockTcpListener = new Mock<ITcpListener>();
            var mockTcpClient = new Mock<ITcpClient>();

            MockComponentFactory.Setup(x => x.BuildTcpListener(It.IsAny<IPAddress>(), It.IsAny<int>(), null))
                .Returns(mockTcpListener.Object);
            MockComponentFactory.Setup(x => x.BuildTcpClient(It.IsAny<string>(), It.IsAny<int>(), null))
                .Returns(mockTcpClient.Object);

            #endregion

            var listener = new Listener("127.0.0.1", 35000, null);        // note: 'localhost' setups up 2 listeners; 0.0.0.0 and "::"
            var returnedListener = listener.Start();

            listener.Stop();

            mockTcpListener.Verify(x => x.Stop(), Times.Once);
        }

        #endregion
    }
}
