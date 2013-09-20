using System;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using sema2012m;

namespace CommonRDF

{
   

    public class TValue
    {
        //public static readonly Hashtable Cach = new Hashtable();(RecordEx)(Cach[id] ?? (Cach[id] = 
        public string Value;
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
                else return;
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
                   SetTargetType(v);
               };
            connect.WhenObjSetted += connectOnWhenObjSetted;
        }
    }
    public enum Role{ Unknown, Data, Object}
    public class QueryTriplet
    {
        public TValue S, P, O;
        public TripletAction Action;
        public static GraphBase Gr;
        public static Action<int> Match;
       
        public QueryTriplet(bool isNewS, bool isNewP, bool isNewO,
            TValue s, TValue p, TValue o)
        {
            S = s;P = p;O = o;
            Action = null;
            //if (!isNewP)
            //    if (!isNewS)
            //        if (!isNewO) Action = TestTriplet;
            //        else Action = SelectObject;
            //    else if (!isNewO) Action = SelectSubject;
            //         else Action = SelectBoth; 
            //else if (!isNewS)
            //    if (!isNewO) Action = SelectPredicate;
            //        else Action = SelectPredicateObject;
            //    else if (!isNewO) Action = SelectPredicateSubject;
            //else Action = SelectAll;
        }

        //#region delegates
        //public void TestTriplet(int next)
        //{
        //    if ((O.IsObj != Role.Data && Gr.GetDirect(S.Value, P.Value).Contains(O.Value)) ||
        //        (O.IsObj != Role.Object && Gr.GetData(S.Value, P.Value).Contains(O.Value)))
        //        Match(next);
        //}

        //private void SelectObject(int next)
        //{
        //    bool isObject=false;//= O.IsObj != Role.Object;
        //    if (O.IsObj != Role.Data)
        //        foreach (string value in Gr.GetDirect(S.Value, P.Value))
        //        {
        //            isObject = true;
        //            O.Value = value;
        //            Match(next);

        //        }
        //    if (isObject) return;
        //    foreach (string value in Gr.GetData(S.Value, P.Value))
        //    {
        //        O.Value = value;
        //        Match(next);
        //    }
        //}

        //private void SelectSubject(int next)
        //{
        //    bool isObj = false;
        //    if (O.IsObj != Role.Data)
        //        foreach (string value in Gr.GetInverse(O.Value, P.Value))
        //        {
        //            isObj = true;
        //            S.Value = value;
        //            Match(next);
        //        }
        //    if (isObj) return;
        //    foreach (string value in  
        //        P.Value == ONames.p_name
        //            ? Gr.SearchByName(O.Value).Where(OnPredicate)
        //            : Gr.GetEntities().Where(OnPredicate))
        //    {
        //        S.Value = value;
        //        Match(next);
        //    }
        //}

        //private bool OnPredicate(string id)
        //{
        //    return Gr.GetData(id, P.Value).Contains(O.Value);
        //}

        //private void SelectBoth(int next)
        //{
        //    bool isNotData = true; // !p.State.HasFlag(TState.Data); - syncronized
        //    bool isObj = false;
        //    if (O.IsObj != Role.Unknown)
        //        isNotData = isObj = O.IsObj == Role.Object;
        //    foreach (string id in Gr.GetEntities())
        //    {
        //        S.Value = id;
        //        if (isNotData)
        //            foreach (var v in Gr.GetDirect(id, P.Value))
        //            {
        //                isObj = true;
        //                O.Value = v;
        //                Match(next);

        //            }
        //        if (isObj) continue;
        //        foreach (var v in Gr.GetData(id, P.Value))
        //        {
        //            O.Value = v;
        //            Match(next);
        //        }
        //    }
        //}

        //private void SelectPredicate(int next)
        //{
        //    bool isNotData = true;
        //    bool isObj = false;
        //    if (O.IsObj != Role.Unknown)
        //        isNotData = isObj = O.IsObj == Role.Object;
        //    if (isNotData)
        //        foreach (PredicateEntityPair pe in Gr.GetDirect(S.Value))
        //        {
        //            if (pe.entity != O.Value) continue;
        //            P.Value = pe.predicate;
        //            Match(next);
        //        }
        //    if (isObj) return;
        //    foreach (var pd in Gr.GetData(S.Value))
        //    {
        //        if (pd.data != O.Value) continue;
        //        P.Value = pd.predicate;
        //        Match(next);
        //    }
        //}

        //private void SelectAll(int i)
        //{
        //    bool isNotData = true;
        //    bool isObj = false;
        //    if (O.IsObj != Role.Unknown)
        //        isNotData = isObj = O.IsObj == Role.Object;
        //    foreach (var id in Gr.GetEntities())
        //    {
        //        S.Value = id;
        //        if (isNotData)
        //            foreach (var dataTriple in Gr.GetDirect(id))
        //            {
        //                P.Value = dataTriple.predicate;
        //                O.Value = dataTriple.entity;
        //                Match(i + 1);
        //            }
        //        if (isObj) continue;
        //        foreach (var dataTriple in Gr.GetData(id))
        //        {
        //            P.Value = dataTriple.predicate;
        //            O.Value = dataTriple.data;
        //            Match(i + 1);
        //        }
        //    }
        //}

        //private void SelectPredicateSubject(int i)
        //{
        //    bool isNotData = true;
        //    bool isObj = false;
        //    if (O.IsObj != Role.Unknown)
        //        isNotData = isObj = O.IsObj == Role.Object;
        //    if (isNotData)
        //        foreach (PredicateEntityPair axe in Gr.GetInverse(O.Value))
        //        {
        //            P.Value = axe.predicate;
        //            S.Value = axe.entity;
        //            Match(i + 1);
        //        }
        //    if (isObj) return;
        //    foreach (PredicateDataTriple axe in Gr.GetData(O.Value))
        //    {
        //        P.Value = axe.predicate;
        //        S.Value = axe.data;
        //        Match(i + 1);
        //    }
        //}

        //private void SelectPredicateObject(int i)
        //{
        //    bool isNotData = true;
        //    bool isObj = false;
        //    if (O.IsObj != Role.Unknown)
        //        isNotData = isObj = O.IsObj == Role.Object;
        //    if (isNotData)
        //        foreach (PredicateEntityPair axe in Gr.GetDirect(S.Value))
        //        {
        //            P.Value = axe.predicate;
        //            O.Value = axe.entity;
        //            Match(i + 1);
        //        }
        //    if (isObj) return;
        //    foreach (var axe in Gr.GetData(S.Value))
        //    {
        //        P.Value = axe.predicate;
        //        O.Value = axe.data;
        //        Match(i + 1);
        //    }
        //}

        //#endregion


    }

    public delegate void TripletAction(int next);
    public class QueryTripletOptional
    {
        public TValue S, P, O;
        public TripletAction Action;
        public bool HasSOptValue, HasPOptValue, HasOOptValue;
        public  static GraphBase Gr;
        public static Action<int> MatchOptional;

        public QueryTripletOptional(bool isNewS, bool isNewP, bool isNewO,
            TValue s, TValue p, TValue o,
            bool hasSOptValue, bool hasPOptValue, bool hasOOptValue)
        {
            S = s; P = p; O = o;
            HasSOptValue = hasSOptValue;
            HasPOptValue = hasPOptValue;
            HasOOptValue=hasOOptValue;
            Action = null;

            //if (!isNewP)
            //    if (!isNewS)
            //        if (!isNewO) Action = null;
            //        else Action = SelectObject;
            //    else if (!isNewO) Action = null;
            //    else Action = null;
            //else if (!isNewS)
            //    if (!isNewO) Action = null;
            //    else Action = null;
            //else if (!isNewO) Action = null;
            //else Action = null;
        }

      //#region delegates optional
      //  private void SelectObject(int next)
      //  {
      //      var known = S.Value;
      //      var unKnown = O;
      //      SelectUnknown(unKnown, known, HasOOptValue, next);
      //  }

      //  private void SelectUnknown(TValue unKnown, string known, bool hasOptValue, int next)
      //  {
      //      if (hasOptValue)
      //      {
      //          MatchOptional(next);
      //          string oldValue = unKnown.Value;
      //          foreach (var newOptV in ( //TODO inverse
      //              unKnown.IsObj == Role.Unknown
      //                  ? Gr.GetData(known, P.Value).Concat(Gr.GetDirect(known, P.Value))
      //                  : unKnown.IsObj == Role.Object
      //                      ? Gr.GetDirect(known, P.Value)
      //                      : Gr.GetData(known, P.Value))
      //              .Where(newOptV => newOptV != oldValue))
      //          {
      //              unKnown.Value = newOptV;
      //              MatchOptional(next);
      //          }
      //          unKnown.Value = oldValue;
      //          return;
      //      }
      //      bool any = false;
      //      foreach (var newOptV in (unKnown.IsObj == Role.Unknown
      //          ? Gr.GetData(known, P.Value).Concat(Gr.GetDirect(known, P.Value))
      //          : unKnown.IsObj == Role.Object
      //              ? Gr.GetDirect(known, P.Value)
      //              : Gr.GetData(known, P.Value)))
      //      {
      //          any = true;
      //          unKnown.Value = newOptV;
      //          MatchOptional(next);
      //      }
      //      if (any) return;
      //      unKnown.Value = string.Empty;
      //      MatchOptional(next);
      //  }

      //  #endregion

    }
}
