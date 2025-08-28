﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiklabChallenge.Core.Shared
{
    public static class AppRoles
    {
        public const string Admin = "Admin";
        public const string Student = "Student";

        public static readonly string[] All = new[] { Admin, Student };
    }
}
