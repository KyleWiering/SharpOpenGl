using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace SharpOpenGl.environment
{
    class EnvironmentController : IEnvironment
    {
        public Spacefield Spacefield { get; private set; }
        
        public Model Model { get; set; }
        public EnvironmentController()
        {
            
        }

        public void Initialize()
        {
            Spacefield = new Spacefield();
            Spacefield.Initialize();

            Model = new Model();
            Model.Initialize();

        }

        public void ExternalRender()
        {
            Spacefield.Render();
        }


        public void Render()
        {
            Model.Render();
        }

        public void Update(double EllapsedTime)
        {
            Model.Update(EllapsedTime);
        }

        void IEnvironment.Destroy()
        {

        }
    }
}
