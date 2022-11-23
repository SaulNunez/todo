using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace todo
{
    internal class InteractiveOutput : IApiOutput
    {
        public Task AddTask(string taskName, string listName)
        {
            throw new NotImplementedException();
        }

        public Task CheckTask(string taskName, string listName)
        {
            throw new NotImplementedException();
        }

        public Task CreateTaskList(string listName)
        {
            throw new NotImplementedException();
        }

        public Task DeleteTask(string taskName, string listName)
        {
            throw new NotImplementedException();
        }

        public Task DeleteTaskList(string listName)
        {
            throw new NotImplementedException();
        }

        public Task ShowList(string listName)
        {
            throw new NotImplementedException();
        }
    }
}
