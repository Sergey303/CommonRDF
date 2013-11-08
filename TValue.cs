using System;
using System.Linq;
using sema2012m;

namespace CommonRDF
{
    public class TValue
    {
        public string Value=string.Empty;
        public object nodeInfo;
        public bool SetNodeInfo;
        public bool? IsObject;
        public event Action<TValue> WhenReplace;
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
                if (double.TryParse(Value, out DoubleValue))
                    return true;
                DateTime dateTime;
                if (DateTime.TryParse(Value, out dateTime))
                {
                    DoubleValue = dateTime.Ticks;
                    return true;
                }
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

        public void SyncIsObjectRole(TValue friend)
        {
            SubscribeIsObjSetted(friend);
            friend.SubscribeIsObjSetted(this);
        }
    }

    public abstract class SparqlBase
    {
        public abstract bool Match();
        public Func<bool> NextMatch;

        protected SparqlBase()
        {
            
        }
       protected SparqlBase(SparqlBase last)
        {
            if(last!=null)
            last.NextMatch = Match;
        }
    }

    /// <summary>
    /// базовый класс для триплета
    /// какого класса создавать триплет определяется при чтении запроса по трём булевым переменным.
    /// </summary>
    public abstract class SparqlTriplet : SparqlBase
    {
        public readonly TValue S, P, O;
        public bool HasNodeInfoS, HasNodeInfoO;
        public bool Any;

        /// <summary>
        /// необходим доступ к графу, для вычисления
        /// </summary>
        public static GraphBase Gr;
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

        protected SparqlTriplet(TValue s, TValue p, TValue o)
        {
            S = s;
            P = p;
            O = o;
            HasNodeInfoS = s.SetNodeInfo;
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
        public SampleTriplet(TValue s, TValue p, TValue o) : base(s, p, o)
        {
            s.SetNodeInfo = true;
        }

        /// <summary>
        /// метод выполняет проверку: соответсвуют ли данным три значения
        /// объектность не известна, поэтому проверить нужно и текстовые и объектные предикаты
        /// субъекта на соответствие с указанным значением предиката.
        /// </summary>
        /// <returns>возвращает тоже, что и следующий Match, если триплет соответсвует данным, ложь если нет.</returns>
        public override bool Match()
        {
            object nodeInfo = HasNodeInfoS ? S.nodeInfo : (S.nodeInfo = Gr.GetNodeInfo(S.Value));
            if (IsObjectRole != null)
                return ((IsObjectRole.Value
                    ? Gr.GetDirect(S.Value, P.Value, nodeInfo)
                    : Gr.GetData(S.Value, P.Value, nodeInfo))
                    .Any(oValue => oValue == O.Value)) && NextMatch();
            if (Gr.GetDirect(S.Value, P.Value, nodeInfo).Any(oValue => oValue == O.Value))
            {
                IsObjectRole = true;
                return NextMatch();
            }
            if (Gr.GetData(S.Value, P.Value, nodeInfo).Any(oValue => oValue == O.Value))
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
        public SelectSubject(TValue s, TValue p, TValue o)
            : base(s, p, o)
        {
            HasNodeInfoO = o.SetNodeInfo;
            o.SetNodeInfo = true;
        }

        /// <summary>
        /// метод выполняет перебор всех вариантов субъектов и для каждого запускает следующий Match
        /// </summary>
        /// <returns></returns>
        public override bool Match()
        {
            Any = false;
            if (IsObjectRole == null || IsObjectRole.Value)
                foreach (string value in Gr.GetInverse(O.Value, P.Value, HasNodeInfoO ? O.nodeInfo : (O.nodeInfo = Gr.GetNodeInfo(O.Value))))
                {
                    IsObjectRole = true;
                    S.Value = value;
                  Any =  NextMatch() || Any;
                }
            if (IsObjectRole!=null && IsObjectRole.Value) return Any;
            foreach (string value in
                P.Value == ONames.p_name
                    ? Gr.SearchByName(O.Value).Where(OnPredicate)
                    : Gr.GetEntities().Where(OnPredicate))
            {
                IsObjectRole = false;
                S.Value = value;
               Any= NextMatch()||Any;
            }
            return Any;
        }
        protected bool OnPredicate(string id)
        {
            return Gr.GetData(id, P.Value).Contains(O.Value);
        }
    }

    /// <summary>
    /// этот триплет создаётся в ситуаци, когда не известен только объект на данном этапе выполнения. 
    /// </summary>
    public class SelectObject : SparqlTriplet
    {
        public SelectObject(TValue s, TValue p, TValue o)
            : base(s, p, o)
        {
            s.SetNodeInfo = true;
        }

        /// <summary>
        /// метод выполняет перебор всех вариантов объектов и для каждого запускает следующий Match
        /// </summary>
        /// <returns></returns>
        public override bool Match()
        {
            Any = false;
            if (IsObjectRole==null|| IsObjectRole.Value)
                foreach (string value in Gr.GetDirect(S.Value, P.Value, HasNodeInfoS ? S.nodeInfo : (S.nodeInfo = Gr.GetNodeInfo(S.Value))))
                {
                    IsObjectRole = true;
                    O.Value = value;
                    Any=NextMatch() || Any;
                }
            if (IsObjectRole!=null && IsObjectRole.Value) return Any;
            foreach (var valueLang in Gr.GetDataLangPairs(S.Value, P.Value, HasNodeInfoS ? S.nodeInfo : (S.nodeInfo = Gr.GetNodeInfo(S.Value))))
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
        public SelectPredicate(TValue s, TValue p, TValue o)
            : base(s, p, o)
        {
            s.SetNodeInfo = true;
        }

        /// <summary>
        /// метод находит все предикаты, известного субъекта, с известным значением, и для каждого запускает следующий Match
        /// </summary>
        /// <returns></returns>
        public override bool Match()
        {
            throw new NotImplementedException();
        }
    }
    public class SelectAllPredicatesBySub :SparqlTriplet{
        public SelectAllPredicatesBySub(TValue s, TValue p, TValue o)
            : base(s, p, o)
        {
            s.SetNodeInfo = true;
        }

        public override bool Match()
        {
            throw new NotImplementedException();
        }
    }
    public class SelectAllSubjects : SparqlBase

    {
        public TValue S;
        public SelectAllSubjects(TValue s)
        {
            S = s;
        }
       
        public override bool Match()
        {
            bool any = false;
            foreach (var id in SparqlTriplet.Gr.GetEntities())
            {
                S.Value = id;
                any = NextMatch() || any;
            }
            return any;
        }
    }

    public class SelectSubjectOpional : SelectSubject
    {
        public SelectSubjectOpional(TValue s, TValue p, TValue o)
            : base(s, p, o)
        {}

        /// <summary>
        /// метод выполняет перебор всех вариантов субъектов и для каждого запускает следующий Match
        /// </summary>
        /// <returns></returns>
        public override bool Match()
        {
            if (base.Match()) return true;
            S.Value = string.Empty;
            return NextMatch();
        }
    }

    /// <summary>
    /// этот триплет создаётся в ситуаци, когда не известен только объект на данном этапе выполнения. 
    /// </summary>
    public class SelectObjectOprtional : SelectObject
    {
        public SelectObjectOprtional(TValue s, TValue p, TValue o)
            : base(s, p, o)
        {
        }

        /// <summary>
        /// метод выполняет перебор всех вариантов объектов и для каждого запускает следующий Match
        /// </summary>
        /// <returns></returns>
        public override bool Match()
        {
            if (base.Match()) return true;
            O.Lang = O.Value = string.Empty;
           return NextMatch();
        }
    }
    public class SelectAllSubjectsOptional : SelectAllSubjects
    {
        public SelectAllSubjectsOptional(TValue s)
            : base(s)
        {}

        public override bool Match()
        {
            if (base.Match()) return true;
            S.Value = string.Empty;
            return NextMatch();
        }
    }

    public class SparqlChain:SparqlBase
    {
        protected Func<bool> start;
        protected SparqlBase last;
        public void Add(params SparqlBase[] nexts)
        {
            if(nexts.Length==0) return;
            if (start == null) start = nexts[0].Match;
            else last.NextMatch = nexts[0].Match;
            for (int i = 1; i < nexts.Length; i++)
                nexts[i - 1].NextMatch = nexts[i].Match;
            
            last=nexts.Last();
            last.NextMatch = () => NextMatch();
        }

        public override bool Match()
        {
            return start != null && start();
        }
    }
}
