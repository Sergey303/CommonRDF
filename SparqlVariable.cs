using System;
using System.Linq;
using sema2012m;

namespace CommonRDF
{
    public class SparqlVariable
    {
        public string Value=string.Empty;
        public bool? IsObject;
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

        public string Lang=string.Empty;
        public double DoubleValue;

        public bool IsDouble
        {
            get
            {
                DateTime dateTime;
                if (DateTime.TryParse(Value, out dateTime))
                {
                    DoubleValue = dateTime.Ticks;
                    return true;
                }
                if (double.TryParse(Value, out DoubleValue))
                    return true;
                if(double.TryParse(Value.Replace(".",","), out DoubleValue))
                return true;
                
                return false;
            }
        }

        public void SetTargetType(bool value)
        {
            if (IsObject != null)
                if (IsObject.Value != value)
                    throw new Exception("to object sets data");
                else return;
            IsObject = value;
            //if(whenObjSetted!=null)
            //    whenObjSetted(value);
        }

        public void SubscribeIsObjSetted(SparqlVariable connect)
        {
            Action<bool> connectOnWhenObjSetted = null;
           connectOnWhenObjSetted= 
               v=> { 
                   connect.whenObjSetted -= connectOnWhenObjSetted;
                   SetTargetType(v);
               };
            connect.WhenObjSetted += connectOnWhenObjSetted;
        }

        public void SyncIsObjectRole(SparqlVariable friend)
        {
            SubscribeIsObjSetted(friend);
            friend.SubscribeIsObjSetted(this);
        }
    }

    public abstract class SparqlNodeBase
    {
        public abstract bool Match();
        public Func<bool> NextMatch;

        /// <summary>
        /// необходим доступ к графу, для вычисления
        /// </summary>
        public static GraphBase Gr;

        protected SparqlNodeBase()
        {
            
        }
       protected SparqlNodeBase(SparqlNodeBase last)
        {
            if(last!=null)
            last.NextMatch = Match;
       }
    }

    /// <summary>
    /// базовый класс для триплета
    /// какого класса создавать триплет определяется при чтении запроса по трём булевым переменным.
    /// </summary>
    public abstract class SparqlTriplet : SparqlNodeBase
    {
        public readonly SparqlVariable S, P, O;
       // public bool HasNodeInfoS, HasNodeInfoO;
        public bool Any;

        /// <summary>
        /// если известен один, то известен и другой, к сожалению у второго значение не утанавливается,
        /// и все триплеты с последним могут остаться не известными.
        /// </summary>
        public bool? IsObjectRole
        {
            get
            {
                return P.IsObject ?? (O.IsObject);
            }
            set
            {
                if (IsObjectRole != null) return;
                P.IsObject = O.IsObject = value;
            }
        }

        protected SparqlTriplet(SparqlVariable s, SparqlVariable p, SparqlVariable o)
        {
            S = s;
            P = p;
            O = o;
        }

    }
    /// <summary>
    ///  все три значения(будь то константы или параметры) не пусты
    /// при порверки данного триплета нужно проверить соттветсвие субъекта предиката объекта данным.
    /// При этомне известен вид прредиката: обектное или текстовое свойство. При константном  предикате
    /// предполагается в будущем объектность известна. Такая ситуация возможна, огда предикат параметр,
    /// встретился и был установлен ранее в запросе.
    /// </summary>
    public class SampleTriplet: SparqlTriplet
    {
        public SampleTriplet(SparqlVariable s, SparqlVariable p, SparqlVariable o) : base(s, p, o)
        {
        }

        /// <summary>
        /// метод выполняет проверку: соответсвуют ли данным три значения
        /// объектность не известна, поэтому проверить нужно и текстовые и объектные предикаты
        /// субъекта на соответствие с указанным значением предиката.
        /// </summary>
        /// <returns>возвращает тоже, что и следующий Match, если триплет соответсвует данным, ложь если нет.</returns>
        public override bool Match()
        {
            if (IsObjectRole != null)
                return ((IsObjectRole.Value
                    ? Gr.GetDirect(S.Value, P.Value)
                    : Gr.GetData(S.Value, P.Value))
                    .Any(oValue => oValue == O.Value)) && NextMatch();
            if (Gr.GetDirect(S.Value, P.Value).Any(oValue => oValue == O.Value))
            {
                IsObjectRole = true;
                return NextMatch();
            }
            if (Gr.GetData(S.Value, P.Value).Any(oValue => oValue == O.Value))
            {
                IsObjectRole = false;
                return NextMatch();
            }
            return false;
        }
    }
    /// <summary>
    /// этот триплет создаёттся в ситуаци, когда не известен только субъект на данном этапе выполнения. 
    /// </summary>
    public class SelectSubject : SparqlTriplet
    {
        public SelectSubject(SparqlVariable s, SparqlVariable p, SparqlVariable o)
            : base(s, p, o)
        {
         //   HasNodeInfoO = o.SetNodeInfo;
        //    o.SetNodeInfo = true;
        }

        /// <summary>
        /// метод выполняет перебор всех вариантов субъектов и для каждого запускает следующий Match
        /// </summary>
        /// <returns></returns>
        public override bool Match()
        {
            Any = false;
            if (IsObjectRole == null || IsObjectRole.Value)
                foreach (string value in Gr.GetInverse(O.Value, P.Value))
                {
                    IsObjectRole = true;
                    S.Value = value;
                  Any =  NextMatch() || Any;
                }
            if (IsObjectRole!=null && IsObjectRole.Value) return Any;
            foreach (string value in Gr.GetSubjectsByData(O.Value, P.Value))
            {
                IsObjectRole = false;
                S.Value = value;
               Any= NextMatch()||Any;
            }
            return Any;
        }
      
    }

    /// <summary>
    /// этот триплет создаётся в ситуаци, когда не известен только объект на данном этапе выполнения. 
    /// </summary>
    public class SelectObject : SparqlTriplet
    {
        public SelectObject(SparqlVariable s, SparqlVariable p, SparqlVariable o)
            : base(s, p, o)
        {
        }

        /// <summary>
        /// метод выполняет перебор всех вариантов объектов и для каждого запускает следующий Match
        /// </summary>
        /// <returns></returns>
        public override bool Match()
        {
            Any = false;
            if (IsObjectRole==null|| IsObjectRole.Value)
                foreach (string value in Gr.GetDirect(S.Value, P.Value))
                {
                    IsObjectRole = true;
                    O.Value = value;
                    Any=NextMatch() || Any;
                }
            if (IsObjectRole!=null && IsObjectRole.Value) return Any;
            foreach (var valueLang in Gr.GetDataLangPairs(S.Value, P.Value))
            {
                IsObjectRole = false;
                O.Value = valueLang.data;
                O.Lang = valueLang.lang ?? string.Empty;
                Any = NextMatch() || Any;
            }
            return Any;
        }
    }
    /// <summary>
    /// аналогично двум предыдущим
    /// </summary>
    public class SelectPredicate : SparqlTriplet
    {
        public SelectPredicate(SparqlVariable s, SparqlVariable p, SparqlVariable o)
            : base(s, p, o)
        {
        }

        /// <summary>
        /// метод находит все предикаты, известного субъекта, с известным значением, и для каждого запускает следующий Match
        /// </summary>
        /// <returns></returns>
        public override bool Match()
        {
            bool any = false;
            foreach (var pair in Gr.GetDirect(S.Value).Where(pair => pair.entity == O.Value))
            {
                P.Value = pair.predicate;
                any = NextMatch() || any;
            }
            foreach (var pair in Gr.GetData(S.Value).Where(pair => pair.data == O.Value))
            {
                P.Value = pair.predicate;
                any = NextMatch() || any;
            }
            return any;
        }
    }

    public class SelectPredicateByObj : SparqlTriplet
    {
        public SelectPredicateByObj(SparqlVariable s, SparqlVariable p, SparqlVariable o) : base(s, p, o)
        {
        }

        public override bool Match()
        {
            bool any=false;
            foreach (var predicateEntityPair in Gr.GetInverse(O.Value))
            {
                P.Value = predicateEntityPair.predicate;
                P.IsObject = O.IsObject = true;
                S.Value = predicateEntityPair.entity;
                any = NextMatch() || any;
            }
            foreach (var predicateEntityPair in Gr.GetSubjectsByData(O.Value))
            {
                P.Value = predicateEntityPair.predicate;
                P.IsObject = O.IsObject = false;
                S.Value = predicateEntityPair.entity;
                any = NextMatch() || any;
            }
            return any;
        }
    }

    public class SelectAllPredicatesBySub :SparqlTriplet{
        public SelectAllPredicatesBySub(SparqlVariable s, SparqlVariable p, SparqlVariable o)
            : base(s, p, o)
        {
        }

        public override bool Match()
        {
            bool any = false;
            foreach (var pair in Gr.GetDirect(S.Value))
            {
                P.Value = pair.predicate;
                P.IsObject = O.IsObject = true;
                O.Value = pair.entity;
                any = NextMatch() || any;
            }
            foreach (var pair in Gr.GetData(S.Value))
            {
                P.Value = pair.predicate;
                O.Value = pair.data; 
                P.IsObject = O.IsObject = false;
                O.Lang = pair.lang;
                any = NextMatch() || any;
            }
            return any;
        }
    }
    public class SelectAllSubjects : SparqlNodeBase

    {
        public SparqlVariable S;
        public SelectAllSubjects(SparqlVariable s)
        {
            S = s;
        }
       
        public override bool Match()
        {
            bool any = false;
            foreach (var id in Gr.GetEntities())
            {
                S.Value = id;
                any = NextMatch() || any;
            }
            return any;
        }
    }


    public abstract class OptionalSparqlTripletBase : SparqlNodeBase
    {
        public SparqlVariable[] Parameters;

        public bool OptionalFailMatch()
        {
            if (Parameters == null) return NextOptionalFailMatch();
            foreach (var parameter in Parameters)
                parameter.Lang = parameter.Value = string.Empty;
            return NextMatch();
        }

        public Func<bool> NextOptionalFailMatch;
      
    }
    public class SampleTripletBaseOptional : OptionalSparqlTripletBase
    {
        public readonly SparqlVariable S, P, O;

        public SampleTripletBaseOptional(SparqlVariable s, SparqlVariable p, SparqlVariable o)
        {
            S = s;
            P = p;
            O = o;
        }

        public override bool Match()
        {
            bool result=false;
            if (IsObjectRole != null)
                result = ((IsObjectRole.Value
                    ? Gr.GetDirect(S.Value, P.Value)
                    : Gr.GetData(S.Value, P.Value))
                    .Any(oValue => oValue == O.Value));
            if (Gr.GetDirect(S.Value, P.Value).Any(oValue => oValue == O.Value))
                IsObjectRole = result = true;
            else if (Gr.GetData(S.Value, P.Value).Any(oValue => oValue == O.Value))
            {
                IsObjectRole = false;
                result = true;
            }
            return result ? NextMatch() : OptionalFailMatch();
        }

        public bool? IsObjectRole
        {
            get
            {
                return P.IsObject ?? (O.IsObject);
            }
            set
            {
               //if (IsObjectRole != null) return;
                P.IsObject = O.IsObject = value;
            }
        }
    }
    public class SelectSubjectOpional : SampleTripletBaseOptional
   {
       public SelectSubjectOpional(SparqlVariable s, SparqlVariable p, SparqlVariable o)
           : base(s, p, o)
       { }
     
       public override bool Match()
       {
           var Any = false;
           bool anyCurrent = false;
           if (IsObjectRole == null || IsObjectRole.Value)
               foreach (string value in Gr.GetInverse(O.Value, P.Value))
               {
                   IsObjectRole = true;
                   S.Value = value;
                   anyCurrent = true;
                   Any = NextMatch() || Any;
               }
           if (IsObjectRole != null && IsObjectRole.Value) return anyCurrent ? Any : OptionalFailMatch();
           foreach (string value in Gr.GetSubjectsByData(O.Value, P.Value))
           {
               IsObjectRole = false;
               S.Value = value;
               anyCurrent = true;
               Any = NextMatch() || Any;
           }
           return anyCurrent ? Any : OptionalFailMatch();;
       }
   }

    public class SelectObjectOprtional : SampleTripletBaseOptional
    {
        public SelectObjectOprtional(SparqlVariable s, SparqlVariable p, SparqlVariable o)
            : base(s, p, o)
        {
        }

        /// <summary>
        /// метод выполняет перебор всех вариантов объектов и для каждого запускает следующий Match
        /// </summary>
        /// <returns></returns>
        public override bool Match()
        {
          var  Any = false;
            bool result = false;
            if (IsObjectRole == null || IsObjectRole.Value)
                foreach (string value in Gr.GetDirect(S.Value, P.Value))
                {
                    IsObjectRole = true;
                    O.Value = value;
                    result = true;
                    Any = NextMatch() || Any;
                }
            if (IsObjectRole != null && IsObjectRole.Value)
                return result ? Any : OptionalFailMatch();
            foreach (var valueLang in Gr.GetDataLangPairs(S.Value, P.Value))
            {
                IsObjectRole = false;
                O.Value = valueLang.data;
                O.Lang = valueLang.lang;
                result = true;
                Any = NextMatch() || Any;
            }
            return result ? Any : OptionalFailMatch();
        }
    }
   public class SelectAllSubjectsOptional : OptionalSparqlTripletBase
   {
       private SparqlVariable S;
        public SelectAllSubjectsOptional(SparqlVariable s)
        {
            S = s;
        }

        public override bool Match()
        {

            bool any = false;
            foreach (var id in Gr.GetEntities())
            {
                S.Value = id;
                any = NextMatch() || any;
            }
            return any;
        }
    }

   public class SelectPredicateOptional : SampleTripletBaseOptional
    {
        public SelectPredicateOptional(SparqlVariable s, SparqlVariable p, SparqlVariable o)
            : base(s, p, o)
        {
        }

        /// <summary>
        /// метод находит все предикаты, известного субъекта, с известным значением, и для каждого запускает следующий Match
        /// </summary>
        /// <returns></returns>
        public override bool Match()
        {
            bool any = false, anyCurrent=false;
            foreach (var pair in Gr.GetDirect(S.Value).Where(pair => pair.entity == O.Value))
            {
                P.Value = pair.predicate;
                anyCurrent = true;
                any = NextMatch() || any;
            }
            foreach (var pair in Gr.GetData(S.Value).Where(pair => pair.data == O.Value))
            {
                P.Value = pair.predicate;
                any = NextMatch() || any;
                anyCurrent = true;
            }
            return anyCurrent ? any : OptionalFailMatch();
        }
    }

    public class StartNode : SparqlNodeBase
    {

        public override bool Match()
        {
            return NextMatch();
        }
    }
  
}
