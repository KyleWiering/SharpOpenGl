using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpOpenGl.environment
{
    interface IEnvironment
    {
        void Initialize();
        void Render();
        void Update(double EllapsedTime);
        void Destroy();
    }
}
