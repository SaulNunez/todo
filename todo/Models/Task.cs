using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace todo.Models;
public record TaskInformation (
    string Title, 
    bool IsCompleted,
    DateTime? DueDate,
    bool InMyDay
    );
