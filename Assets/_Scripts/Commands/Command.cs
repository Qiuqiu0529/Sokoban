using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Commands;

public class Command 
{
    private int maxCommandHistorySize = 50;
    private Stack<Commands.ICommand> commandHistory = new Stack<Commands.ICommand>();
    private Stack<Commands.ICommand> redoStack = new Stack<Commands.ICommand>();
    public void SetMaxCommandHistorySize(int size)
    {
        maxCommandHistorySize = size;
    }
    public void ExecuteCommand(Commands.ICommand command)
    {
        command.Execute();
        AddCommand(command);
    }

    public void AddCommand(Commands.ICommand command)
    {
        commandHistory.Push(command);
        if (commandHistory.Count > maxCommandHistorySize)
        {
            commandHistory.TrimExcess();
        }

        foreach (var item in redoStack)
        {
            item.ClearCommand();
        }
        redoStack.Clear();
    }

    public void ClearAllCommands()
    {
        foreach (var item in redoStack)
        {
            item.ClearCommand();
        }
        commandHistory.Clear();
        redoStack.Clear();
    }

    public void Undo()
    {
        if (commandHistory.Count > 0)
        {
            Commands.ICommand command = commandHistory.Pop();
            command.Undo();
            redoStack.Push(command);
        }
    }

    public void Redo()
    {
        if (redoStack.Count > 0)
        {
            Commands.ICommand command = redoStack.Pop();
            command.Execute();
            commandHistory.Push(command);
        }
    }
}
