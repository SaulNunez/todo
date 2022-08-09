using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace todo
{
    internal interface IApiOutput
    {
        void ShowList(string listName, List<string> tasks);
        void DeleteTask(string taskName);
        void CheckTask(string taskName);
        void AddTask(string taskName);
    }
}
