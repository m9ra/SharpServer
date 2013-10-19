﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SharpServer.Compiling;

namespace SharpServer
{
    public abstract class WebApplication
    {
        public readonly ResponseHandlerProvider HandlerProvider;

        protected abstract ResponseManagerBase createResponseManager();

        protected abstract InputManagerBase createInputManager();

        protected abstract Type[] getHelpers();

        protected WebApplication()
        {
            var helperTypes=getHelpers();
            var webHelpers=new WebMethods(helperTypes);
            HandlerProvider = new ResponseHandlerProvider(webHelpers);
        }
        
        internal ResponseManagerBase CreateResponseManager(){
            var manager=createResponseManager();

            return manager;
        }

        internal InputManagerBase CreateInputManager()
        {
            var manager = createInputManager();

            return manager;
        }
    }
}
