namespace CommonRDF
{
    class Program
    {
        static void Main(string[] args)
        {
          Graph gr=new Graph();
          gr.Load(@"..\..\0001.xml");
            gr.Test();
        }

      
    }
}
