using System;
using System.Runtime.Remoting.Messaging;

namespace CommonRDF

{
   

    public class TValue
    {
        //public static readonly Hashtable Cach = new Hashtable();(RecordEx)(Cach[id] ?? (Cach[id] = 
        public string Value;
        public bool IsNewParameter;
        public bool HasOptValue;
        public bool? IsObj;
        private event Action<bool> whenObjSetted;
        public event Action<bool> WhenObjSetted
        {
            add
            {
                if (whenObjSetted == null) whenObjSetted = value;
                else whenObjSetted += value;
            }
            remove
            {
                whenObjSetted -= value;
            }
        }

        public void SetTargetType(bool value)
        {
            if (IsObj != null)
                if (IsObj.Value != value)
                    throw new Exception("to object sets data");
            IsObj = value;
            if(whenObjSetted!=null)
                whenObjSetted(value);
        }

        public void SubscribeIsObjSetted(TValue connect)
        {
            Action<bool> connectOnWhenObjSetted = null;
           connectOnWhenObjSetted= 
               v=> { 
                   connect.whenObjSetted -= connectOnWhenObjSetted;
                   this.SetTargetType(v);
               };
            connect.WhenObjSetted += connectOnWhenObjSetted;
        }
    }

    public class QueryTriplet
    {
        public TValue S, P, O;
    }
}
