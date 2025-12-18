namespace cfm_frontend.Models
{
    public class WorkRequestFilterModel
    {
        public class LocationModel
        {
            public int id { get; set; }
            public string name { get; set; }
            public int propertyGroupId { get; set; }
        }

        public class ServiceProviderModel
        {
            public int id { get; set; }
            public string name { get; set; }
            public string companyName { get; set; }
        }

        public class WorkCategoryModel
        {
            public int id { get; set; }
            public string name { get; set; }
            public string description { get; set; }
        }

        public class OtherCategoryModel
        {
            public int id { get; set; }
            public string name { get; set; }
            public string categoryType { get; set; }
        }

        public class PriorityLevelModel
        {
            public int id { get; set; }
            public string name { get; set; }
            public string level { get; set; }
        }
    }
}
