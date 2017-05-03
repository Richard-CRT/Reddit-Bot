using System.Collections.Generic;

namespace Reddit_Bot
{
    // JSON Response Object Hierarchies

    public class JsonCommentsRequestContentBase
    {
        public string kind { get; set; }
        public JsonCommentsRequestContentBaseData data { get; set; }
    }

    public class JsonCommentsRequestContentBaseData
    {
        public string modhash { get; set; }
        public IList<JsonCommentsRequestContentBaseDataComment> children { get; set; }
        public string after { get; set; }
        public string before { get; set; }
    }

    public class JsonCommentsRequestContentBaseDataComment
    {
        public JsonCommentsRequestContentBaseDataCommentData data { get; set; }

        public bool isOlderThan(double UTCValue)
        {
            return this.data.created_utc < UTCValue;
        }

        public override bool Equals(object obj)
        {
            JsonCommentsRequestContentBaseDataComment comment = obj as JsonCommentsRequestContentBaseDataComment;
            return this.data.name == comment.data.name;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    public class JsonCommentsRequestContentBaseDataCommentData
    {
        public string body { get; set; }
        public string name { get; set; }
        public double created_utc { get; set; }
    }
}
