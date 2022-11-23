using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using todo.Models;

namespace todo;

public class PipeOutput : IApiOutput
{
    public ApiQueries api;

    public PipeOutput()
    {
        api = null;
    }

    public async Task CreateTaskList(string listName)
    {
        try
        {
            await api.AddTaskList(listName);
            Console.WriteLine("Task list created");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Oops, something went wrong. Error: {e.Message}");
        }
    }

    public async Task DeleteTaskList(string listName)
    {
        try
        {
            await api.DeleteTaskListByName(listName);
            Console.WriteLine("Task list deleted");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Oops, something went wrong. Error: {e.Message}");
        }
    }

    public async Task AddTask(string taskName, string listName)
    {
        try
        {
            await api.CreateTask(taskName, listName);
        } catch(Exception e)
        {
            Console.WriteLine("Oops...something went wrong. 😓");
            Console.WriteLine(e.Message);
        }
        await ShowList(listName);
    }

    public async Task CheckTask(string taskName, string listName)
    {
        await api.CheckTask(listName, taskName);
        await ShowList(listName);
    }

    public async Task DeleteTask(string taskName, string listName)
    {
        await api.DeleteByName(listName, taskName);
        await ShowList(listName);
    }

    public async Task ShowList(string listName)
    {
        Console.WriteLine(listName);
        Console.WriteLine();

        var tasks = await api.GetListAsync(listName);

        foreach (var task in tasks)
        {
            var taskMarked = task.Status == Microsoft.Graph.TaskStatus.Completed ? "x" : "";
            Console.WriteLine($"[{taskMarked}]\t{task.Title}\t{task.DueDateTime}");
        }
    }
}
