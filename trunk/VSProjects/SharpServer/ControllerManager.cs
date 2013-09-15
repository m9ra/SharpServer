﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SharpServer.Networking;

namespace SharpServer
{
    public abstract class ControllerManager
    {
        public abstract void Handle(Client client);
    }
}
