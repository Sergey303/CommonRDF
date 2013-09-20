using System;
using System.Runtime.Remoting.Messaging;

namespace CommonRDF

{
   

    public class TValue
    {
        //public static readonly Hashtable Cach = new Hashtable();(RecordEx)(Cach[id] ?? (Cach[id] = 
        public string Value;
        public Role IsObj;
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
            if (IsObj == Role.Data && value
                || IsObj == Role.Object && !value)
                    throw new Exception("to object sets data");
            IsObj = value ? Role.Object : Role.Data;
            if(whenObjSetted!=null)
                whenObjSetted(value);
        }

        public void SubscribeIsObjSetted(TValue connect)
        {
            Action<bool> connectOnWhenObjSetted = null;
           connectOnWhenObjSetted= 
               v=> { 
                   connect.whenObjSetted -= connectOnWhenObjSetted;
                   SetTargetType(v);
               };
            connect.WhenObjSetted += connectOnWhenObjSetted;
        }
    }
    public enum Role{ Unknown, Data, Object}
    public struct QueryTriplet
    {
        public TValue S, P, O;
        public TripletAction Action;
    }

    public delegate bool TripletAction(TValue s, TValue p, TValue o, int i);
    public struct QueryTripletOptional
    {
        public TValue S, P, O;
        public TripletAction Action;
        public bool HasSOptValue, HasPOptValue, HasOOptValue;
    }
}
