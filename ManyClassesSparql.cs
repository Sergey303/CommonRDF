<<<<<<< HEAD
﻿using System;
using CommonRDF;

namespace Sparql
{

    // =============== Структуры для Sparql ================
    

    /// <summary>
    /// Переменные и константы
    /// Переменные сохраняются в свой массив их имена в другой (в соответсвующем порядке).
    /// Константы в третий и только что бы объединить константы с одинаковым значением,
    /// после чтения массив констант не нужен. наверно лучще список.
    /// </summary>
    public class SparqlValue
   {   
        /// <summary>
        /// значение. будь то константа объектная или текстовая или значение параметра.
        /// если параметр ещё не полуил значения, то остаётся null.
        /// </summary>
        public string Value;

        /// <summary>
        /// offset или hash или т.п.
        /// </summary>
        public long Cache;

        /// <summary>
        ///  для субъктов всегда истина. 
        /// вычисляется при чтении запроса
        /// если вычислена, то действует во всех содержащих триплетах.
        /// </summary>
        public bool? IsObject;
   }

    /// <summary>
    /// базовый класс для триплета
    /// какого класса создавать триплет определяется при чтении запроса по трём булевым переменным.
    /// </summary>
    public abstract class SparqlTriplet
    {
        public SparqlValue subject, predicate, obj;
        public bool option = false;

        /// <summary>
        /// содержит ли значение, установленное ранее в опциональном триплете.
        /// при этом значения считается что нет, и перебор будет произведён.
        /// это нужно, в случае, если этот триплет опционален и НЕ должен перекрыть старое опциональное значение,
        /// и дублировать.
        /// можно заменить на проверку subject.Value==null
        /// </summary>
        public bool hasOptionalValueSubject = false, 
            hasOptionalValuePredicate = false, 
            hasOptionalValueObject = false;
        /// <summary>
        /// необходим доступ к графу, для вычисления
        /// </summary>
        public static GraphBase Gr;
        /// <summary>
        /// если известен один, то известен и другой, к сожалению у второго значение не утанавливается,
        /// и все триплеты с последним могут остаться не известными.
        /// </summary>
        public bool? IsObject{ get
        {
            return predicate.IsObject ?? (obj.IsObject);
        }}
        public abstract bool Match(Func<int, IReceiver, bool> nextMatch, int next);
    }
    /// <summary>
    ///  все три значения(будь то константы или параметры) не пусты
    /// при порверки данного триплета нужно проверить соттветсвие субъекта предиката объекта данным.
    /// При этомне известен вид прредиката: обектное или текстовое свойство. При константном  предикате
    /// предполагается в будущем объектность известна. Такая ситуация возможна, огда предикат параметр,
    /// встретился и был установлен ранее в запросе.
    /// </summary>
    public class Sample:SparqlTriplet
    {
    /// <summary>
        /// метод выполняет проверку: соответсвуют ли данным три значения
        /// объектность не известна, поэтому проверить нужно и текстовые и объектные предикаты
        /// субъекта на соответствие с указанным значением предиката.
        /// </summary>
        /// <param name="nextMatch">в случае соответсвия триплета данным будет вызван этот метод - проверка следующего
        /// предполагается, что тут будет Query.Match</param>
        /// <param name="next">номер следующего параметра = i+1</param>
        /// <returns>возвращает тоже, что и следующий Match, если триплет соответсвует данным, ложь если нет.</returns>
        public override bool Match(Func<int, IReceiver, bool> nextMatch, int next)
        {
            return true;
        }
    }
    /// <summary>
    /// этот триплет создаёттся в ситуаци, когда не известен только субъект на данном этапе выполнения. 
    /// </summary>
    public class SelectSubject : SparqlTriplet
    {
        /// <summary>
        /// метод выполняет перебор всех вариантов субъектов и для каждого запускает следующий Match
        /// </summary>
        /// <param name="nextMatch"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        public override bool Match(Func<int, IReceiver, bool> nextMatch, int next)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// этот триплет создаётся в ситуаци, когда не известен только объект на данном этапе выполнения. 
    /// </summary>
    public class SelectObject : SparqlTriplet
    {
        /// <summary>
        /// метод выполняет перебор всех вариантов объектов и для каждого запускает следующий Match
        /// </summary>
        /// <param name="nextMatch"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        public override bool Match(Func<int, IReceiver, bool> nextMatch, int next)
        {
            throw new NotImplementedException();
        }
    }
    /// <summary>
    /// аналогично двум предыдущим
    /// </summary>
    public class SelectPredicate : SparqlTriplet
        {
            /// <summary>
            /// метод находит все предикаты, известного субъекта, с известным значением, и для каждого запускает следующий Match
            /// </summary>
            /// <param name="nextMatch"></param>
            /// <param name="next"></param>
            /// <returns></returns>
            public override bool Match(Func<int, IReceiver, bool> nextMatch, int next)
            {
                throw new NotImplementedException();
            }
        }

    
}
=======
﻿using System;
using CommonRDF;

namespace Sparql
{

    // =============== Структуры для Sparql ================
    

    /// <summary>
    /// Переменные и константы
    /// Переменные сохраняются в свой массив их имена в другой (в соответсвующем порядке).
    /// Константы в третий и только что бы объединить константы с одинаковым значением,
    /// после чтения массив констант не нужен. наверно лучще список.
    /// </summary>
    public class SparqlValue
   {   
        /// <summary>
        /// значение. будь то константа объектная или текстовая или значение параметра.
        /// если параметр ещё не полуил значения, то остаётся null.
        /// </summary>
        public string Value;

        /// <summary>
        /// offset или hash или т.п.
        /// </summary>
        public long Cache;

        /// <summary>
        ///  для субъктов всегда истина. 
        /// вычисляется при чтении запроса
        /// если вычислена, то действует во всех содержащих триплетах.
        /// </summary>
        public bool? IsObject;
   }

    /// <summary>
    /// базовый класс для триплета
    /// какого класса создавать триплет определяется при чтении запроса по трём булевым переменным.
    /// </summary>
    public abstract class SparqlTriplet
    {
        public SparqlValue subject, predicate, obj;
        public bool option = false;

        /// <summary>
        /// содержит ли значение, установленное ранее в опциональном триплете.
        /// при этом значения считается что нет, и перебор будет произведён.
        /// это нужно, в случае, если этот триплет опционален и НЕ должен перекрыть старое опциональное значение,
        /// и дублировать.
        /// можно заменить на проверку subject.Value==null
        /// </summary>
        public bool hasOptionalValueSubject = false, 
            hasOptionalValuePredicate = false, 
            hasOptionalValueObject = false;
        /// <summary>
        /// необходим доступ к графу, для вычисления
        /// </summary>
        public static GraphBase Gr;
        /// <summary>
        /// если известен один, то известен и другой, к сожалению у второго значение не утанавливается,
        /// и все триплеты с последним могут остаться не известными.
        /// </summary>
        public bool? IsObject{ get
        {
            return predicate.IsObject ?? (obj.IsObject);
        }}
        public abstract bool Match(Func<int, IReceiver, bool> nextMatch, int next);
    }
    /// <summary>
    ///  все три значения(будь то константы или параметры) не пусты
    /// при порверки данного триплета нужно проверить соттветсвие субъекта предиката объекта данным.
    /// При этомне известен вид прредиката: обектное или текстовое свойство. При константном  предикате
    /// предполагается в будущем объектность известна. Такая ситуация возможна, огда предикат параметр,
    /// встретился и был установлен ранее в запросе.
    /// </summary>
    public class Sample:SparqlTriplet
    {
    /// <summary>
        /// метод выполняет проверку: соответсвуют ли данным три значения
        /// объектность не известна, поэтому проверить нужно и текстовые и объектные предикаты
        /// субъекта на соответствие с указанным значением предиката.
        /// </summary>
        /// <param name="nextMatch">в случае соответсвия триплета данным будет вызван этот метод - проверка следующего
        /// предполагается, что тут будет Query.Match</param>
        /// <param name="next">номер следующего параметра = i+1</param>
        /// <returns>возвращает тоже, что и следующий Match, если триплет соответсвует данным, ложь если нет.</returns>
        public override bool Match(Func<int, IReceiver, bool> nextMatch, int next)
        {
            return true;
        }
    }
    /// <summary>
    /// этот триплет создаёттся в ситуаци, когда не известен только субъект на данном этапе выполнения. 
    /// </summary>
    public class SelectSubject : SparqlTriplet
    {
        /// <summary>
        /// метод выполняет перебор всех вариантов субъектов и для каждого запускает следующий Match
        /// </summary>
        /// <param name="nextMatch"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        public override bool Match(Func<int, IReceiver, bool> nextMatch, int next)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// этот триплет создаётся в ситуаци, когда не известен только объект на данном этапе выполнения. 
    /// </summary>
    public class SelectObject : SparqlTriplet
    {
        /// <summary>
        /// метод выполняет перебор всех вариантов объектов и для каждого запускает следующий Match
        /// </summary>
        /// <param name="nextMatch"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        public override bool Match(Func<int, IReceiver, bool> nextMatch, int next)
        {
            throw new NotImplementedException();
        }
    }
    /// <summary>
    /// аналогично двум предыдущим
    /// </summary>
    public class SelectPredicate : SparqlTriplet
        {
            /// <summary>
            /// метод находит все предикаты, известного субъекта, с известным значением, и для каждого запускает следующий Match
            /// </summary>
            /// <param name="nextMatch"></param>
            /// <param name="next"></param>
            /// <returns></returns>
            public override bool Match(Func<int, IReceiver, bool> nextMatch, int next)
            {
                throw new NotImplementedException();
            }
        }

    
}
>>>>>>> 5b07a7d99da1a84c4d159acd03a3aad69dc94ef7
