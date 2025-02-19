using Microsoft.Graph.Models;

namespace todo;

public static class PrettyPrint{
    public static void Print(TodoTask task){
        Console.WriteLine(task.Title);
        Console.WriteLine(task.Status);
        Console.WriteLine(task.DueDateTime.ToDateTimeOffset());
        Console.WriteLine(task.ReminderDateTime.ToDateTimeOffset());
    }

    static void IndividualTaskItem(TodoTask task){
        Console.Write(task.Status);
        Console.Write('\t');
        Console.Write(task.Title);
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