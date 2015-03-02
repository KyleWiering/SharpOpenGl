using System;
using System.IO;

namespace SharpOpenGl
{
    class ModelList
    {
        public ModelList()
        {
            Models = null;
        }

        private Model[] Models;

        private Model LoadModel(string ModelPath, string ModelName)
        {
            Model model = new Model();
            model.ModelName = ModelName;
            string []ModelInput = File.ReadAllLines(ModelPath + "/" + ModelName + ".mod");
            foreach (string ModelInputString in ModelInput)
            {
                string[] VectorPieces = ModelInputString.Split(new char[' '], System.StringSplitOptions.RemoveEmptyEntries);

            }


            return model;
        }
    }
}
