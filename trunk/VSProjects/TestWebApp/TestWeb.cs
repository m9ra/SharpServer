﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SharpServer;

namespace TestWebApp
{
    class TestWeb : WebApplication
    {
        protected override ControllerManager createManager()
        {
            var manager = new SimpleControllerManager(this, "www/");
            manager.AddAll();
            //      manager.AddPath("application.haml");

            return manager;
        }

        protected override Type[] getHelpers()
        {
            return new[]{
                typeof(WebHelper)
            };
        }
    }
}
