using System;

namespace CommonRDF

{
    [Flags]
    public enum TState :byte
    {
        Nan=0,
        Data = 1,
        Obj = 2,
        HasItem = 4,
        IsOpen = 8,
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
        public string Value;
        public TState State=TState.Nan;
        public bool IsNewParameter;
        public void SetTargetTypeObj()
        {
            if (State.HasFlag(TState.Data))
                throw new Exception("to object sets data");
            State |= TState.Obj;
        }

        public void SetTargetTypeData()
        {
            if (State.HasFlag(TState.Obj))
                throw new Exception("to data sets object");
            State |= TState.Data;
        }
        
        public RecordEx Item
        {
            get
            {
                if (State.HasFlag(TState.HasItem)) return item;
                State |= TState.HasItem;
                 return item = ItemCtor(Value);
            }
        }

        public void SetValue(string value)
        {
           
            if (ReferenceEquals(value, Value)) return;
            IsNewParameter = false;
            Value = value;
            State &= ~TState.HasItem;//ItemCtor(value);
        }
        
        public void SetValue(string value, RecordEx itm)
        {
           // if (ReferenceEquals(value, item)) return;
            //State &= ~TState.IsNewParametr;
            IsNewParameter = false;
            Value = value;
            item = itm;
            State |= TState.HasItem;
        }


    }

    public class QueryTriplet
    {
        public TValue S, P, O;
    }
}
