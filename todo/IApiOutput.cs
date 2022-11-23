using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using todo.Models;

namespace todo
{
    internal interface IApiOutput
    {
        Task CreateTaskList(string listName);
        Task DeleteTaskList(string listName);
        Task AddTask(string taskName, string listName);
        Task CheckTask(string taskName, string listName);
        Task DeleteTask(string taskName, string listName);
        Task ShowList(string listName);
    }
}
