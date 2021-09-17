using Crestron.RAD.Common.Helpers;
using Crestron.SimplSharp;

namespace Crestron.RAD.Drivers.Displays
{
    using System;

    using Crestron.RAD.Common;
    using Crestron.RAD.Common.BasicDriver;
    using Crestron.RAD.Common.Enums;
    using Crestron.RAD.Common.Transports;
    using Crestron.RAD.DeviceTypes.Display;

    public class PanasonicTHXXCQ1UProtocol : ADisplayProtocol
    {
        private CTimer _pollVolumeTimer;// = new CTimer(PollVolumeCallback, Timeout.Infinite);
        private CTimer _authenticatedTimer;// = new CTimer(PollVolumeCallback, Timeout.Infinite);

        private ISerialTransport _transportDriver;

        private string _defaultPassword { get; set; }
        private string _defaultUsername { get; set; }

        private string _password { get; set; }
        private string _username { get; set; }

        private string _currentUsername;
        private string _currentPassword;

        private bool _authenticated;
        private bool _authenticating;

        private byte _swapFlag = 0;

        public PanasonicTHXXCQ1UProtocol(ISerialTransport transportDriver, byte id)
            : base(transportDriver, id)
        {
            _transportDriver = transportDriver;
            ResponseValidation = new AbsoluteValidator(Id, ValidatedData, this);
            ValidatedData.PowerOnPollingSequence = new[] 
            { 
                StandardCommandsEnum.PowerPoll, 
                StandardCommandsEnum.InputPoll,
                StandardCommandsEnum.VolumePoll,
                StandardCommandsEnum.MutePoll
            };

            _pollVolumeTimer = new CTimer(PollVolumeCallback, Timeout.Infinite);
            _authenticatedTimer = new CTimer(AuthenticatedCallback, Timeout.Infinite);
            PollingInterval = 10000;
        }

        public void Initialize(DisplayRootObject DisplayData, string defaultUsername, string defaultPassword){
            base.Initialize(DisplayData);
            _defaultUsername = defaultUsername;
            _defaultPassword = defaultPassword;
            _currentUsername = defaultUsername;
            _currentPassword = defaultPassword;
        }

        protected override void ConnectionChanged(bool connection)
        {
            AuthenticationEvent(false);
            base.ConnectionChanged(connection);
        }

        public void SetUsername(string username)
        {
            _username = username;
            if (!String.IsNullOrEmpty(_username))
            {
                _currentUsername = _username;
            }
        }

        public void SetPassword(string password)
        {
            _password = password;
            if (!String.IsNullOrEmpty(_password))
            {
                _currentPassword = _password;
            }
        }

        public void StartAuthenticatedTimer()
        {
            _authenticatedTimer.Reset(1000);
        }

        private void AuthenticatedCallback(object o)
        {
            _authenticating = false;
            AuthenticationEvent(true);
        }

        internal void AuthenticationEvent(bool isAuthenticated)
        {
            _authenticated = isAuthenticated;
            FireEvent(DisplayStateObjects.Authentication, _authenticated);
        }

        protected override void Poll()
        {
#if DEBUG
            CrestronConsole.PrintLine("Poll():_authenticating:{0}, WarmingUp:{1}, CoolingDown:{2}, PowerIsOn:{3}", _authenticating, WarmingUp, CoolingDown, PowerIsOn);
#endif
            if (_authenticating)
            {
                // Do nothing
            }
            else if (WarmingUp)
            {
                // Do nothing
            }
            else if (CoolingDown)
            {
                // Do nothing
            }
            else
            {
                base.Poll();
            }
        }

        public void PollPower()
        {
            SendCustomPriorityCommand("PowerPoll",
                DisplayData.CrestronSerialDeviceApi.Api.Feedback.PowerFeedback.GroupHeader,
                CommonCommandGroupType.Power,
                CommandPriority.Special,
                StandardCommandsEnum.NotAStandardCommand);
        }

        public void PollInput()
        {
            SendCustomPriorityCommand("InputPoll",
                DisplayData.CrestronSerialDeviceApi.Api.Feedback.InputFeedback.GroupHeader,
                CommonCommandGroupType.Input,
                CommandPriority.Special,
                StandardCommandsEnum.NotAStandardCommand);
        }

        public void PollMute()
        {
            SendCustomPriorityCommand("MutePoll",
                DisplayData.CrestronSerialDeviceApi.Api.Feedback.MuteFeedback.GroupHeader,
                CommonCommandGroupType.Mute,
                CommandPriority.Special,
                StandardCommandsEnum.NotAStandardCommand);
        }

        public void PollVolume()
        {
            // Poll with each attempt to set
            _pollVolumeTimer.Reset(250);
        }

        private void PollVolumeCallback(object o)
        {
            SendCustomPriorityCommand("VolumePoll",
                DisplayData.CrestronSerialDeviceApi.Api.Feedback.VolumeFeedback.GroupHeader,
                CommonCommandGroupType.Volume,
                CommandPriority.Special,
                StandardCommandsEnum.NotAStandardCommand);
        }

        internal void SendCustomPriorityCommand(string name, string message, CommonCommandGroupType groupType,
            CommandPriority priority, StandardCommandsEnum commandEnum)
        {
            CommandSet command = new CommandSet(name, message, groupType,
                null, false, priority, commandEnum);
            SendCommand(command);
        }

        protected override bool PrepareStringThenSend(CommandSet commandSet)
        {
            // Wrap all commands in header and footer
#if DEBUG
            CrestronConsole.PrintLine("PrepareStringThenSend:{0}", Utility.FormatAsciiHex(commandSet.Command));
#endif
            commandSet.Command = String.Format("{0}{1}{2}", "\u0002", commandSet.Command, "\u0003");
            return base.PrepareStringThenSend(commandSet);
        }

        protected override void DeConstructPower(string response)
        {
#if DEBUG
            CrestronConsole.PrintLine("DeConstructPower:[{0}]", Utility.FormatAsciiHex(response));
#endif
            switch (response)
            {
                case "PON":
                case "POF":
                    {
                        PollPower();
                        base.DeConstructPower("");
                        break;
                    }
                case "0":
                case "1":
                    {
                        base.DeConstructPower(response);
                        break;
                    }
            }
        }

        protected override void DeConstructInput(string response)
        {
#if DEBUG
            CrestronConsole.PrintLine("DeConstructInput:{0}", Utility.FormatAsciiHex(response));
#endif
            switch (response)
            {
                case "IMS":
                    {
                        PollInput();
                        break;
                    }
                default:
                    {
                        // Let base handle identifying input
                        base.DeConstructInput(response);
                        break;
                    }
            }
        }

        protected override void DeConstructVolume(string response)
        {
#if DEBUG
            CrestronConsole.PrintLine("DeConstructVolume:{0}", Utility.FormatAsciiHex(response));
#endif
            try
            {
                if (string.IsNullOrEmpty(response))
                {
                    // Ignore
                    return;
                }
                else if (response.Equals("AVL"))
                {
                    PollVolume();
                }
                else
                {
                    int value = Convert.ToInt32(response);
                    if (value > 100)
                    {
                        return;
                    }

                    base.DeConstructVolume(value.ToString());
                }
            }
            catch (Exception e)
            {
                Log(String.Format("DeConstructVolume: Expected VolumePercent Feedback is not a valid numerical value. Reason={0}", e.Message));
                return;
            }
        }

        protected override void DeConstructMute(string response)
        {
#if DEBUG
            CrestronConsole.PrintLine("DeConstructMute:{0}", Utility.FormatAsciiHex(response));
#endif
            switch (response)
            {
                case "AMT":
                    {
                        PollMute();
                        break;
                    }
                default:
                    {
                        // Let base handle identifying Mute
                        base.DeConstructMute(response);
                        break;
                    }
            }
        }

        public override void SetVolume(uint volumeLev)
        {
#if DEBUG
            CrestronConsole.PrintLine("SetVolume({0})", volumeLev);
#endif
            if (volumeLev > 100)
            {
                volumeLev = 100;
            }

            UnscaledVolumeIs = volumeLev;
            UnscaledRampingVolumeIs = UnscaledVolumeIs;

            string formattedVolume = Convert.ToString(volumeLev);

            Commands command = DisplayData.CrestronSerialDeviceApi.Api.StandardCommands[StandardCommandsEnum.Vol];
            var volumeParameter = ParameterHelper.GetFirstValidParameter(command);
            formattedVolume = ParameterHelper.FormatValue(formattedVolume, volumeParameter);
            string volumeParameterTag = "!$[" + volumeParameter.Id + "]";
            string modifiedCommand = ParameterHelper.ReplaceParameter(command.Command, volumeParameterTag, formattedVolume);

            CommandSet volumeCommand = BuildCommand(StandardCommandsEnum.Vol, CommonCommandGroupType.Volume,
                CommandPriority.Normal, "Volume " + Convert.ToString(volumeLev), modifiedCommand);

            if (volumeCommand != null)
            {
                SendCommand(volumeCommand);
            }

            // Force volume event
            DeConstructVolume(volumeLev.ToString());
        }

        public void SendUsername()
        {
            _authenticating = true;
#if DEBUG
            CrestronConsole.PrintLine("SendUsername:_currentUsername:{0}", _currentUsername);
#endif
            Transport.Send(String.Format("{0}\u000D", _currentUsername), null);
        }

        public void SendPassword()
        {
            _authenticating = true; 
#if DEBUG
            CrestronConsole.PrintLine("SendPassword:_currentPassword:{0}", _currentPassword);
#endif
            Transport.Send(String.Format("{0}\u000D", _currentPassword), null);
        }

        public void SendNewPassword()
        {
            _authenticating = true;
            if (String.IsNullOrEmpty(_password))
            {
                _currentPassword = _defaultPassword;
            }
            else
            {
                _currentPassword = _password;
            }
#if DEBUG
            CrestronConsole.PrintLine("SendNewPassword:_currentPassword:{0}", _currentPassword);
#endif
            Transport.Send(String.Format("{0}\u000D", _currentPassword), null);
        }

        public void swapLogins()
        {
            // The display doesn't tell us whether it is the username or password that is wrong
            // Swap between the 4 different possiblities
            _authenticating = true;
#if DEBUG
            CrestronConsole.PrintLine("swapLogins:{0}", _swapFlag);
#endif
            switch (_swapFlag)
            {
                case 0:
                    {
                        _swapFlag = 1;
                        _currentUsername = _defaultUsername;
                        _currentPassword = _defaultPassword;
                        break;
                    }
                case 1:
                    {
                        _swapFlag = 2;
                        _currentUsername = _defaultUsername;
                        if (String.IsNullOrEmpty(_password))
                        {
                            _currentPassword = _defaultPassword;
                        }
                        else
                        {
                            _currentPassword = _password;
                        }
                        break;
                    }
                case 2:
                    {
                        _swapFlag = 3;
                        _currentPassword = _defaultPassword;
                        if (String.IsNullOrEmpty(_username))
                        {
                            _currentUsername = _defaultUsername;
                        }
                        else
                        {
                            _currentUsername = _username;
                        }
                        break;
                    }
                case 3:
                    {
                        _swapFlag = 0;
                        if (String.IsNullOrEmpty(_username))
                        {
                            _currentUsername = _defaultUsername;
                        }
                        else
                        {
                            _currentUsername = _username;
                        }

                        if (String.IsNullOrEmpty(_password))
                        {
                            _currentPassword = _defaultPassword;
                        }
                        else
                        {
                            _currentPassword = _password;
                        }
                        break;
                    }
                default:
                    {
                        _swapFlag = 1;
                        _currentUsername = _defaultUsername;
                        _currentPassword = _defaultPassword;
                        break;
                    }
            }

            if (_currentPassword == _defaultPassword)
            {
                if (!String.IsNullOrEmpty(_password))
                {
                    _currentPassword = _password;
                }
            }
            else
            {
                _currentPassword = _defaultPassword;
            }
        }
    }

    public class AbsoluteValidator : ResponseValidation
    {
        public bool PowerOffIssued;
        private PanasonicTHXXCQ1UProtocol _protocol;

        public AbsoluteValidator(byte id, DataValidation dataValidation, PanasonicTHXXCQ1UProtocol protocol)
            : base(id, dataValidation)
        {
            Id = id;
            DataValidation = dataValidation;
            _protocol = protocol;
        }

        public override ValidatedRxData ValidateResponse(string response, CommonCommandGroupType commandGroup)
        {
            ValidatedRxData validatedData = new ValidatedRxData(false, string.Empty);
#if DEBUG
            CrestronConsole.PrintLine("ValidateResponse:response:({0}) {1}", (CommonCommandGroupType)commandGroup, Utility.FormatAsciiHex(response));
#endif
            if (response.StartsWith("\u000D"))
            {
                response = response.Remove(0,1);
            }

            if(response.StartsWith("\u000A"))
            {
                response = response.Remove(0,1);
            }

            if (response == "Login:")
            {
                _protocol.SendUsername();
                validatedData.CommandGroup = CommonCommandGroupType.Login;
                validatedData.Data = response;
                validatedData.Ready = true;
                return validatedData;
            }
            else if (response == "Password:")
            {
                _protocol.SendPassword();
                validatedData.CommandGroup = CommonCommandGroupType.Login;
                validatedData.Data = response;
                validatedData.Ready = true;
                return validatedData;
            }
            else if (response.StartsWith("Login incorrect"))
            {
                _protocol.swapLogins();
                validatedData.CommandGroup = CommonCommandGroupType.Login;
                validatedData.Data = response;
                validatedData.Ready = true;
                return validatedData;
            }
            else if (response.Contains("Input new password:"))
            {
                _protocol.SendNewPassword();
                validatedData.CommandGroup = CommonCommandGroupType.Login;
                validatedData.Data = response;
                validatedData.Ready = true;
                return validatedData;
            }
            else if (response.Contains("Input new password one more time:"))
            {
                _protocol.SendNewPassword();
                validatedData.CommandGroup = CommonCommandGroupType.Login;
                validatedData.Data = response;
                validatedData.Ready = true;
                return validatedData;
            }
            else if (response == "OK\u000D\u000A")
            {
                // OK is recieved both when logging in with Default password and submitting a new password
                _protocol.StartAuthenticatedTimer();
                validatedData.CommandGroup = CommonCommandGroupType.Login;
                validatedData.Data = response;
                validatedData.Ready = true;
                validatedData.Ignore = true;
                return validatedData;
            }
            else if(response.StartsWith("\u0002"))
            {
                if (response.Contains("\u0003"))
                {
                    // Remove \x02 (header)
                    response = RemoveHeader(response, DataValidation.Feedback.Header);
                    // Remove \x03 (delim)
                    response = response.Substring(0, response.IndexOf("\u0003"));

                    validatedData.Ready = true;

                    // NAK
                    if (response.Equals("ER401"))
                    {
#if DEBUG
                        CrestronConsole.PrintLine("Found ER401");
#endif
                        validatedData.CommandGroup = CommonCommandGroupType.AckNak;
                        validatedData.Data = DataValidation.NakDefinition;
                        validatedData.Ready = true;
                        validatedData.Ignore = true;
                        return validatedData;
                    }

                    // "ACK" is just echoing the command
                    switch ((CommonCommandGroupType)commandGroup)
                    {
                        case CommonCommandGroupType.Power:
                            {
                                if (response.Contains(DataValidation.PowerFeedback.GroupHeader))
                                {
                                    response = RemoveHeader(response, DataValidation.PowerFeedback.GroupHeader + ":");
                                }
                                
                                validatedData.CommandGroup = CommonCommandGroupType.Power;
                                validatedData.Data = response;

                                break;
                            }
                        case CommonCommandGroupType.Input:
                            {
                                if (response.Contains(DataValidation.InputFeedback.GroupHeader))
                                {
                                    response = RemoveHeader(response, DataValidation.InputFeedback.GroupHeader + ":");
                                }

                                validatedData.CommandGroup = CommonCommandGroupType.Input;
                                validatedData.Data = response;

                                break;
                            }
                        case CommonCommandGroupType.Volume:
                            {
                                if (response.Contains(DataValidation.VolumeFeedback.GroupHeader))
                                {
                                    response = RemoveHeader(response, DataValidation.VolumeFeedback.GroupHeader + ":");
                                }

                                validatedData.CommandGroup = CommonCommandGroupType.Volume;
                                validatedData.Data = response;

                                break;
                            }
                        case CommonCommandGroupType.Mute:
                            {
                                if (response.Contains(DataValidation.MuteFeedback.GroupHeader))
                                {
                                    response = RemoveHeader(response, DataValidation.MuteFeedback.GroupHeader + ":");
                                }

                                validatedData.CommandGroup = CommonCommandGroupType.Mute;
                                validatedData.Data = response;

                                break;
                            }
                        case CommonCommandGroupType.Other:
                            {
                                if (response.Contains("STV")) // Channel
                                {
                                    validatedData.CommandGroup = CommonCommandGroupType.AckNak;
                                    validatedData.Data = DataValidation.AckDefinition;
                                    validatedData.Ignore = true;
                                }

                                break;
                            }

                        default:{
                            CrestronConsole.PrintLine("ValidateResponse:Unhandled response:({0}){1}",
                                (CommonCommandGroupType) commandGroup,
                                Utility.FormatAsciiHex(response));
                            // If unhandled, pass to base
                            return base.ValidateResponse(response, commandGroup);
                        }
                    }
                }
            }
            else if(response.EndsWith("\u000D")){
                validatedData.Ready = true;
                validatedData.Data = string.Empty;
                validatedData.Ignore = true;
            }
            else if(response.EndsWith("\u000A")){
                validatedData.Ready = true;
                validatedData.Data = string.Empty;
                validatedData.Ignore = true;
            }
            else if (response.Length > 164){
                validatedData.Ready = false;
                validatedData.Data = string.Empty;
                validatedData.Ignore = true;
            }

            return validatedData;
        }
    }

    public static class Utility
    {
        public static string FormatAsciiHex(string str)
        {
            char[] charValues = str.ToCharArray();
            string hexOutput = "";
            int value;
            foreach (char eachChar in charValues)
            {
                value = Convert.ToInt32(eachChar);
                if ((value >= 0x20) && (value < 0x7F))
                {
                    hexOutput += String.Format("{0}", (char)value);
                }
                else
                {
                    hexOutput += String.Format("\\x{0:X2}", value);
                }
            }

            return hexOutput;
        }
    }
}

