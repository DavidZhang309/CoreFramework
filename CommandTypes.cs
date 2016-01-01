using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoreFramework
{
    public interface ICommandHandler
    {
        void SetCommand(CommandConsole console, string[] args);
    }

    public class CommandValue : ICommandHandler
    {
        public void SetCommand(CommandConsole console, string[] args)
        {
            if (args.Length == 0)
                console.Print("=" + Value);
            else
                Value = args[0];
        }

        public string Value { get; set; }
    }

    public class EventCmdArgs : EventArgs
    {
        public EventCmdArgs(CommandConsole console, string[] args)
        {
            ConsoleCaller = console;
            Arguments = args;
        }

        public CommandConsole ConsoleCaller { get; private set; }
        public string[] Arguments { get; private set; }
    }
    public class EventCommand : ICommandHandler
    {
        public event EventHandler<EventCmdArgs> OnCommand;

        public EventCommand()
        { }

        public EventCommand(Action<object, EventCmdArgs> func)
        {
            OnCommand += new EventHandler<EventCmdArgs>(func);
        }

        public void SetCommand(CommandConsole console, string[] args)
        {
            if (OnCommand != null) OnCommand(this, new EventCmdArgs(console, args));
        }
    }
}
