using System;
using System.Collections.ObjectModel;
using System.Xml.Serialization;
using System.Windows;
using System.Collections.Generic;
using System.Diagnostics;

namespace CustomCommandsForCortana.VoiceCommandService.Model
{
    
    public sealed class CustomCommand
    {
        public string Name { get; set; }
        public string Example { get; set; }
        public string ListenFor { get; set; }
        public string Feedback { get; set; }
        public string BatchCommand { get; set; }

        public CustomCommand()
        {
            this.Name = "";
            this.Example = "";
            this.ListenFor = "";
            this.Feedback = "";
            this.BatchCommand = "";
        }

        public string Summary
        {
            get
            {
                return $"Command \"{this.Name}\"\n"
                    + $"Example \"{this.Example}\"\n"
                    + $"ListenFor \"{this.ListenFor}\"\n"
                    + $"Feedback \"{this.Feedback}\"\n"
                    + $"BatchCommand \"{this.BatchCommand}\"";
            }
        }

        public string GenerateXMLDefinition
        {
            get
            {
                return $"<Command Name=\"{this.Name}\">\n"
                    + $"<Example>{this.Example}</Example>\n"
                    + $"<ListenFor>{this.ListenFor}</ListenFor>\n"
                    + $"<Feedback>{this.Feedback}</Feedback>\n"
                    + $"<Navigate/>\n"
                    + $"</Command>\n";
            }
        }

        public void Execute()
        {
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = $"/C {this.BatchCommand}";
            process.Start();
        }
    }

    public sealed class CommandViewModel
    {
        private IEnumerable<CustomCommand> Commands;

        public IEnumerable<CustomCommand> GetCommands
        {
            get{ return Commands; }
            set { Commands = value; }
        }
    }
}