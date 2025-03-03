using System.Drawing;
using Microsoft.Graph.Models;
using Pastel;

namespace todo;

public static class PrettyPrint{
    public static void Print(TodoTask task){
        Console.WriteLine(task.Title);

        Console.Write("Status: ");
        Console.Write('\t');
        Console.WriteLine(task.Status);

        if(task.DueDateTime != null)
        {
            Console.Write("Due Date:");
            Console.Write('\t');
            Console.WriteLine(DateOnly.FromDateTime(task.DueDateTime.ToDateTimeOffset().DateTime));
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
            if (task.DueDateTime.ToDateTime() < DateTime.Now)
            {
                Console.Write($"{task.DueDateTime.ToDateTime().ToString().Pastel(Color.Red)}");
            } 
            else
            {
                Console.Write(task.DueDateTime.ToDateTime());
            }
            
        }
        Console.WriteLine();
    }

    public static void Print(TodoTaskCollectionResponse todoTaskCollection)
    {
        foreach (var task in todoTaskCollection.Value)
        {
            IndividualTaskItem(task);
        }
    }

    public static void Print(TodoTaskListCollectionResponse listCollectionResponse)
    {
        foreach(var list in listCollectionResponse.Value)
        {
            Console.WriteLine(list.DisplayName);
        }
    }
}