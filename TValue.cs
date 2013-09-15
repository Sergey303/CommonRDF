namespace CommonRDF
{
    public class TValue
    {
        public static Graph gr;
        public static RecordEx ItemCtor(string id)
        {
            RecordEx r;
            gr.Dics.TryGetValue(id, out r);
            return r;
        }
        //public static readonly Hashtable Cach = new Hashtable();(RecordEx)(Cach[id] ?? (Cach[id] = 
        public RecordEx item;
        public bool IsNewParametr;
        public string Value;
        public bool IsOpen;
        public bool IsData;

        public RecordEx Item
        {
            get { return ItemCtor(Value); }
        }

        public void SetValue(string value)
        {
           
            if (ReferenceEquals(value, Value)) return;
            IsNewParametr = false;
            Value = value;
           
              //  item = null;//ItemCtor(value);
        }
        
        public void SetValue(string value, RecordEx itm)
        {
           // if (ReferenceEquals(value, item)) return;
            IsNewParametr = false;
            Value = value;
            item = itm;
        }


    }

    public class QueryTriplet
    {
        public TValue S, P, O;
    }
}
