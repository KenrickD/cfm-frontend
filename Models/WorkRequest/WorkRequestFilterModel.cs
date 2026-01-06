namespace cfm_frontend.Models.WorkRequest
{
    public class WorkRequestFilterModel
    {
        public class LocationModel
        {
            public int idProperty { get; set; }
            public string propertyName { get; set; }
            public string cityName { get; set; }
            public int idPropertyType { get; set; }
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
