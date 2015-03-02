using System;
using System.Windows.Forms;
using OpenTK;

namespace SharpOpenGl
{
    class ButtonHandler
    {
        public Vector3 AxisMovement{get;set;}
        public Vector3 AxisRotation{get;set;} 

        public ButtonHandler()
        {
            AxisMovement = new Vector3(0.0f, 0.0f, 0.0f);
            AxisRotation = new Vector3(0.0f, 0.0f, 0.0f);
        }

        public void KeyDown(object sender, KeyEventArgs e)
        {
            Console.Out.WriteLine("Down: " + e.KeyCode);
            switch (e.KeyCode)
            {
                case Keys.W:
                    AxisMovement = new Vector3(AxisMovement.X, AxisMovement.Y, -.1f);
                break;
                case Keys.S:
                    AxisMovement = new Vector3(AxisMovement.X, AxisMovement.Y, .1f);
                break;

                case Keys.A:
                    AxisRotation = new Vector3(AxisRotation.X, 0.05f, AxisRotation.Z);
                break;
                case Keys.D:
                    AxisRotation = new Vector3(AxisRotation.X, -0.05f, AxisRotation.Z);
                break;

                case Keys.Q:
                    AxisMovement = new Vector3(-.1f, AxisMovement.Y, AxisMovement.Z);
                break;
                case Keys.E:
                    AxisMovement = new Vector3(.1f, AxisMovement.Y, AxisMovement.Z);
                break;

                case Keys.X:
                    AxisMovement = new Vector3(AxisMovement.X, -.1f, AxisMovement.Z);
                break;
                case Keys.Z:
                    AxisMovement = new Vector3(AxisMovement.X, .1f, AxisMovement.Z);
                break;
            }
        }

        public void KeyUp(object sender, KeyEventArgs e)
        {
            Console.Out.WriteLine("Up: " + e.KeyCode);
            switch (e.KeyCode)
            {
                case Keys.W:
                    AxisMovement = new Vector3(AxisMovement.X, AxisMovement.Y, 0.0f);
                    break;
                case Keys.S:
                    AxisMovement = new Vector3(AxisMovement.X, AxisMovement.Y, 0.0f);
                    break;

                case Keys.A:
                    AxisRotation = new Vector3(AxisRotation.X, 0.0f, AxisRotation.Z);
                    break;
                case Keys.D:
                    AxisRotation = new Vector3(AxisRotation.X, 0.0f, AxisRotation.Z);
                    break;

                case Keys.Q:
                    AxisMovement = new Vector3(0.0f, AxisMovement.Y, AxisMovement.Z);
                    break;
                case Keys.E:
                    AxisMovement = new Vector3(0.0f, AxisMovement.Y, AxisMovement.Z);
                    break;

                case Keys.X:
                    AxisMovement = new Vector3(AxisMovement.X, 0.0f, AxisMovement.Z);
                    break;
                case Keys.Z:
                    AxisMovement = new Vector3(AxisMovement.X, 0.0f, AxisMovement.Z);
                    break;
            }
        }

    }
}
