using System;

namespace CommonRDF

{
    public enum TargetType :byte
        {
           unkn, data,obj
        }
    public class TValue
    {
        public static Graph gr;
        public static RecordEx ItemCtor(string id)
        {
            RecordEx item;
                gr.Dics.TryGetValue(id, out item);

            return item;
        }
        //public static readonly Hashtable Cach = new Hashtable();(RecordEx)(Cach[id] ?? (Cach[id] = 
        public RecordEx item;
        public bool IsNewParametr;
        public string Value;
        public bool IsOpen;
        public bool HasCashItem;
        
        public TargetType AsTargetType;

        public void SetTargetTypeObj()
        {
            if (AsTargetType.HasFlag(TargetType.data))
                throw new Exception("to object sets data");
            AsTargetType = TargetType.obj;
        }

        public void SetTargetTypeData()
        {
            if (AsTargetType.HasFlag(TargetType.obj))
                throw new Exception("to data sets object");
            AsTargetType = TargetType.data;
        }
        
        public RecordEx Item
        {
            get
            {
                if (HasCashItem) return item;
                HasCashItem = true;
                 return item = ItemCtor(Value);
            }
        }

        public void SetValue(string value)
        {
           
            if (ReferenceEquals(value, Value)) return;
            IsNewParametr = false;
            Value = value;
            HasCashItem = false; //ItemCtor(value);
        }
        
        public void SetValue(string value, RecordEx itm)
        {
           // if (ReferenceEquals(value, item)) return;
            IsNewParametr = false;
            Value = value;
            item = itm;
            HasCashItem = true;
        }


    }

    public class QueryTriplet
    {
        public TValue S, P, O;
    }
}
