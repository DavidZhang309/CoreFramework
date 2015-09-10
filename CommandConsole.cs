using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoreFramework
{
    public class CommandConsole
    {
        private string history = "";

        private Dictionary<string, ICommandHandler> commands;

        public CommandConsole()
        {
            commands = new Dictionary<string, ICommandHandler>();
            Prefix = " >";
            StdoutPrefix = "[Console]";
        }
        public bool PrintToStdout { get; set; }
        public string Prefix { get; set; }
        public string StdoutPrefix { get; set; }

        public void RegisterCommand(string commandName, ICommandHandler handle)
        {
            commands.Add(commandName, handle);
        }
        public void Print(string msg)
        {
            history += Prefix + msg + "\n";
            if (PrintToStdout) System.Console.WriteLine(StdoutPrefix + Prefix + msg);
        }
        public void Call(string command, bool printCommand)
        {
            string trimmed = command.Trim();
            int cmdNameIndex = trimmed.IndexOf(' '); 
            string cmdName = "";
            //get command name
            if (cmdNameIndex == -1)
                cmdName = trimmed;
            else
                cmdName = trimmed.Substring(0, trimmed.IndexOf(' '));

            if (printCommand) Print(trimmed);

            //check and execute
            if (commands.ContainsKey(cmdName))
            {
                string[] args = cmdNameIndex == -1 ? new string[] {} : trimmed.Substring(cmdNameIndex + 1).Split();
                try
                {
                    commands[cmdName].SetCommand(this, args);
                }
                catch (Exception ex)
                {
                    Print("Command handler for '" + cmdName + "' threw an unexpected error\n\t" + ex.Message);
                }
            }
            else if (printCommand)
            {
                Print("Command '" + cmdName + "' does not exist");
            }
        }
    }
}
