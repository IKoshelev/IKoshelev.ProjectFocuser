using EnvDTE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IKoshelev.ProjectFocuser
{
    public static class Util
    {
        public static bool IsUnloaded(this Project proj)
        {
            var isUnloaded = string.Compare(EnvDTE.Constants.vsProjectKindUnmodeled, proj.Kind, StringComparison.OrdinalIgnoreCase) == 0;

            return isUnloaded;
        }
    }
}
