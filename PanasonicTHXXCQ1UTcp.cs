using Crestron.RAD.DeviceTypes.Display;

namespace Crestron.RAD.Drivers.Displays
{
    using Crestron.RAD.Common.Interfaces;
    using Crestron.RAD.Common.Transports;
    using Crestron.SimplSharp;
    using Crestron.SimplSharp.Reflection;
    using System.Linq;
    using Crestron.RAD.Common.Enums;
    using System;
    using Crestron.RAD.Common.BasicDriver;
    
    public class PanasonicTHXXCQ1UTcp : ABasicVideoDisplay, ITcp, IAuthentication, IAuthentication2
    {
        private SimplTransport _transport;
        private PanasonicTHXXCQ1UProtocol _protocol;

        public PanasonicTHXXCQ1UTcp()
        {
            try
            {
                // Any logic that references capabilities/new features within the constructor must be in a 
                // seperate method for this try/catch to catch the exception if this assembly is loaded
                // on a system without these references.
                AddCapabilities();
            }
            catch (TypeLoadException)
            {
                // This exception would happen if this driver was loaded on a system
                // running RADCommon without ITcp2 / ICapability.
            }
        }

        private void AddCapabilities()
        {
            // Adds the Tcp2 capability to allow applications to use a hostname when
            // initializing the driver.
            Tcp2Capability tcp2Capability = new Tcp2Capability(Initialize);
            Capabilities.RegisterInterface(typeof(ITcp2), tcp2Capability);
        }

        public void Initialize(IPAddress ipAddress, int port)
        {
            var tcpTransport = new TcpTransport
            {
                EnableAutoReconnect = EnableAutoReconnect,
                EnableLogging = InternalEnableLogging,
                CustomLogger = InternalCustomLogger,
                EnableRxDebug = InternalEnableRxDebug,
                EnableTxDebug = InternalEnableTxDebug
            };

            tcpTransport.Initialize(ipAddress, port);
            ConnectionTransport = tcpTransport;

            _protocol = new PanasonicTHXXCQ1UProtocol(ConnectionTransport, Id);
            _protocol.EnableLogging = InternalEnableLogging;
            _protocol.CustomLogger = InternalCustomLogger;
            _protocol.StateChange += StateChange;
            _protocol.RxOut += SendRxOut;
            DisplayProtocol = _protocol;
            _protocol.Initialize(DisplayData, DefaultUsername, DefaultPassword);
        }

        public void Initialize(string ipAddress, int port)
        {
            var tcpTransport = new TcpTransport
            {
                EnableAutoReconnect = EnableAutoReconnect,
                EnableLogging = InternalEnableLogging,
                CustomLogger = InternalCustomLogger,
                EnableRxDebug = InternalEnableRxDebug,
                EnableTxDebug = InternalEnableTxDebug
            };

            tcpTransport.Initialize(ipAddress, port);
            ConnectionTransport = tcpTransport;

            _protocol = new PanasonicTHXXCQ1UProtocol(ConnectionTransport, Id);
            _protocol.EnableLogging = InternalEnableLogging;
            _protocol.CustomLogger = InternalCustomLogger;
            _protocol.StateChange += StateChange;
            _protocol.RxOut += SendRxOut;
            DisplayProtocol = _protocol;
            _protocol.Initialize(DisplayData, DefaultUsername, DefaultPassword);
        }

        protected override object FakeFeedbackForStandardCommand(Crestron.RAD.Common.Enums.StandardCommandsEnum command,
                                                                 Crestron.RAD.Common.Enums.CommonCommandGroupType commandGroup)
        {
            // This skips faking feedback. The only thing that needs fake feedback is volume and this is handled.
            return null;
        }

        #region IAuthentication Members

        public override string PasswordKey
        {
            set
            {
                base.PasswordKey = value;
            }
        }

        public override string UsernameKey
        {
            set
            {
                base.UsernameKey = value;
            }
        }

        #endregion

        #region IAuthentication2 Members

        public override void OverridePassword(string password)
        {
            _protocol.SetPassword(password);
        }

        public override void OverrideUsername(string username)
        {
            _protocol.SetUsername(username);
        }

        #endregion
    }
}

       