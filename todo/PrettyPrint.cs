using System.Drawing;
using Microsoft.Graph.Models;
using Pastel;

namespace todo;

public static class PrettyPrint{
    public static void Print(TodoTask? task){
        if(task == null){
            return;
        }

        Console.WriteLine(task.Title);

        Console.Write("Status: ");
        Console.Write('\t');
        Console.WriteLine(task.Status);

        if(task.DueDateTime != null)
        {
            Console.Write("Due Date:");
            Console.Write('\t');
            Console.WriteLine(DateOnly.FromDateTime(task.DueDateTime.ToDateTimeOffset().LocalDateTime));
        }
        if(task.ReminderDateTime != null)
        {
            Console.Write("Reminder Date: ");
            Console.Write('\t');
            Console.WriteLine(task.ReminderDateTime.ToDateTimeOffset().LocalDateTime);
        }

        Console.WriteLine("Notes:");
        if(task.Body != null){
            Console.WriteLine(task.Body?.Content);
        }
    }

    static void IndividualTaskItem(TodoTask task){
        if(task.Status == Microsoft.Graph.Models.TaskStatus.Completed)
        {
            Console.Write("[x]");
        }
        else
        {
            Console.Write("[ ]");
        }
        Console.Write('\t');
        Console.Write(task.Title);
        if(task.DueDateTime != null)
        {
            Console.Write('\t');
            // ToDateTime() drops the timezone, and Graph returns UTC, so comparing it
            // against DateTime.Now was wrong by the local UTC offset.
            var dueDate = task.DueDateTime.ToDateTimeOffset();
            if (dueDate < DateTimeOffset.Now)
            {
                Console.Write($"{dueDate.LocalDateTime.ToString().Pastel(Color.Red)}");
            } 
            else
            {
                Console.Write(dueDate.LocalDateTime);
            }
            
        }
        Console.WriteLine();
    }

    public static void Print(IReadOnlyList<TodoTask> tasks)
    {
        foreach (var task in tasks)
        {
            IndividualTaskItem(task);
        }
    }

    public static void Print(IReadOnlyList<TodoTaskList> lists)
    {
        foreach(var list in lists)
        {
            Console.WriteLine(list.DisplayName);
        }
    }
}
