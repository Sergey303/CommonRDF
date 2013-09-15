using System.Collections;
using System.Collections.Generic;

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
    public class Axe:HashSet<string>
    {
        public PropertyType Direction;

        public Axe(IEnumerable<string> vsList):base(vsList)
        {
            
        }
    }
    public class RecordEx
    {
        public string Id;
        public Hashtable direct;
        public Hashtable inverse;
        public Hashtable data;

        public Axe this[string value, bool isDirect]
        {
            get
            {
                if (isDirect)
                {
                    object o = (data[value] ?? direct[value]);
                    if(o!=null)
                    return o as Axe;
                }
                else
                {
                    object o = (inverse[value]);
                    if (o != null)
                        return o as Axe;
                }
                return null;
            }
        }
    }

    
 public   enum PropertyType:byte
    {
         dir, data, inv
    }

    // =============== Структуры для Sparql ================
    public class DescrVar
    {
        public bool isEntity = true;
        public string varName;
        public string varValue;
    }
    public enum TripletVid { op, dp }
    public class TVariable
    {
        public bool isVariable;
        public string value;
        public int index;
    }
    public class Sample
    {
        public TripletVid vid;
        public int firstunknown;
        public TVariable subject, predicate, obj;
        public bool option = false;
    }
}

