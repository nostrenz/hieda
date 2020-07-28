namespace Hieda.Entity.Attribute
{
	[System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property)]
	public class Collumn : System.Attribute
	{
		private string name;
		private string datatype;
		private string options;
		private bool mapped;

		public Collumn(string name, string datatype, string options=null, bool mapped=true)
		{
			this.name = name;
			this.datatype = datatype;
			this.options = options;
			this.mapped = mapped;
		}

		public string Name
		{
			get { return name; }
		}

		public string Datatype
		{
			get { return this.datatype; }
		}

		public string Options
		{
			get { return this.options; }
		}

		public bool IsMapped
		{
			get { return this.mapped; }
		}
	}
}
