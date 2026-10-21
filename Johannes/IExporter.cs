using System;
using System.Collections.Generic;
using System.Text;

namespace Johannes
{
	public interface IExporter
	{
		void Paragraph(string styleId, List<ParagraphRun> runs);
		void FinishExport();
	}

	public abstract class AbstractExporter : IExporter
	{
		public abstract void Paragraph(string styleId, List<ParagraphRun> runs);
		public abstract void FinishExport();
	}
}
