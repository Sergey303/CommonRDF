using System.Collections.Generic;
using System.Linq;

namespace CommonRDF
{
    // Четверка, получаемая из тройки (триплета). Поле vid {0|1|2} обозначает вид квада (direct, inverse, data)
    // entity - идентификатор сущности. Для нулевого и второго вариантов - это субъект, для первого - объект
    // predicate - идентификатор предиката триплета
    // rest - строка, которая либо означает сущность, дргого конца триплета (варианты 0|1) или данные (вариант 2).
    // Предполагается, что поток триплетов преобразуется в поток квадов, причем объектный триплет преобразуется в два квада, триплет с данными - в один
    public struct Quad
    {
        public Quad(int vid, string entity, string predicate, string rest)
        {
            this.vid = vid;
            this.entity = entity;
            this.predicate = predicate;
            this.rest = rest;
        }
        public int vid;
        public string entity;
        public string predicate;
        public string rest;
    }
    // В поле rest для данных - сохраняется константа и дополнительные квалификаторы. Пока поддерживается только язык
    // Для языковых спецификаторов используем синтетическую строчку вида "данные@lang". При этом @ должен находиться не далее 6 символов от конца @US-en 
    public class Axe
    {
        public string predicate;
        public string[] variants;
    }
    // Это стуктура, которая ставится в соответствие идентификатору сущности. В ней собираются все исходящие, входящие дуги и поля данных 
    public struct RecordEx
    {
        public string rtype;
        public Axe[] direct;
        public Axe[] inverse;
        public Axe[] data;


        public IEnumerable<string> DirectAndDataAxeValues(string predicate, TValue parameter)
        {
            var pre = direct.FirstOrDefault(d=>d.predicate==predicate);
            if (pre!=null)
                foreach (var value in pre.variants)
                    yield return value;
            else
            {
                pre = data.FirstOrDefault(d=>d.predicate==predicate);
                if (pre==null ) yield break;//|| !pre.variants.Any()
                parameter.IsData = true;
                foreach (var value in pre.variants)
                    yield return value;
            }
        }
        public IEnumerable<string> InverseAxeValues(string predicate)
        {
            var pre = inverse.FirstOrDefault(d => d.predicate == predicate);
            if (pre == null) yield break;
            foreach (var value in pre.variants)
                yield return value;
        }


        public bool DirectAxeContains(string predicate, TValue parameter)
        {
            if (parameter.IsData)
            {
                var pre1 = data.FirstOrDefault(d => d.predicate == predicate);
                return pre1 != null && pre1.variants.Contains(parameter.Value);
            }
            var pre = direct.FirstOrDefault(d => d.predicate == predicate);
            return pre != null && pre.variants.Contains(parameter.Value);
        }
        public bool InverseAxeContains(string predicate, string value)
        {
            var pre = inverse.FirstOrDefault(d => d.predicate == predicate);
            return pre != null && pre.variants.Contains(value);
        }
    }

    // =============== Структуры для Sparql ================
    // Переменные выстраиваются в структуру DescrVar[] в порядке появления в Sparql-конструкциях, значения динамически 
    // появляются в процессе вычислений 
    public class DescrVar
    {
        public bool isEntity = true;
        public string varName;
        public string varValue;
    }
    public enum TripletVid { op, dp }
    // Все значения, появляющиеся в строчках запросов или пременные или константы. Индекс - позиция в массиве DescrVar[] 
    public class TVariable
    {
        public bool isVariable;
        public string value;
        public int index;
    }
    // Кострукция строчки запроса: вид утверждения, индекс первой еще не вычисленной переменной, остальное - очевидно
    public class Sample
    {
        public TripletVid vid;
        public int firstunknown;
        public TVariable subject, predicate, obj;
        public bool option = false;
    }
}
