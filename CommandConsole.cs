using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace CoreFramework
{
    public enum VerboseTag { Error, Warning, Info }

    public class CommandConsole
    {
        private string history = "";

        private Dictionary<string, ICommandHandler> commands;
        public TextWriter Output { get; private set; }
        public string Prefix { get; set; }
        public bool PrintTimestamp { get; set; }
        public VerboseTag VerboseLevel { get; set; }

        public CommandConsole()
        {
            commands = new Dictionary<string, ICommandHandler>();
            Output = Console.Out;
            Prefix = " >";
        }

        private void Exec(object sender, EventCmdArgs args)
        {
            if (args.Arguments.Length == 1)
                Execute(args.Arguments[0]);
            else
                Print("Usage: exec [file_path]");
        }
        public void RegisterDefaultCommands()
        {
            RegisterCommand("exec", new EventCommand(new Action<object, EventCmdArgs>(Exec)));
        }
        public void RegisterCommand(string commandName, ICommandHandler handle)
        {
            commands.Add(commandName, handle);
        }
        public string[] GetCommandList()
        {
            return commands.Keys.ToArray();
        }
        public ICommandHandler GetCommand(string name)
        {
            return commands[name];
        }
        public void Execute(string path)
        {
            foreach (string line in File.ReadAllLines(path))
                Call(line, true, true);
        }
        public void Print(string msg)
        {
            string resultMsg = Prefix + msg;
            if (PrintTimestamp)
                resultMsg = string.Format("[{0}]{1}", DateTime.Now, resultMsg);
            history += resultMsg;
            Output.WriteLine(resultMsg);
        }
        public void Print(VerboseTag tag, string msg, bool printTag)
        {
            if (tag <= VerboseLevel)
            {
                if (printTag)
                    Print("[" + tag + "]" + msg);
                else
                    Print(msg);
            }

        }

        protected string[] ParseArgs(string arg)
        {
            bool isQuote = false;
            int lastArg = 0;
            bool isArg = false;
            List<string> args = new List<string>();
            string buffer = "";

            for (int i = 0; i < arg.Length; i++)
            {
                if (arg[i] == '\"')
                {
                    isQuote = !isQuote;
                }
                else if (!isQuote && arg[i] == ' ') //space seperator outside quote
                    isArg = true;
                else
                    buffer += arg[i];

                if (i == arg.Length - 1)
                    isArg = true;

                if (isArg && buffer.Length != 0)
                {
                    args.Add(buffer);
                    buffer = "";
                    lastArg = i - 1;
                    isArg = false;
                }
            }
            return args.ToArray();
        }

        public void Call(string command, bool printInput, bool printCommand)
        {
            string trimmed = command.Trim();
            int cmdNameIndex = trimmed.IndexOf(' '); 
            string cmdName = "";
            //get command name
            if (cmdNameIndex == -1)
                cmdName = trimmed;
            else
                cmdName = trimmed.Substring(0, trimmed.IndexOf(' '));

            if (printInput) Print(trimmed);

            //check and execute
            if (commands.ContainsKey(cmdName))
            {
                string[] args = cmdNameIndex == -1 ? new string[0] {} : ParseArgs(trimmed.Substring(cmdNameIndex + 1));
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
