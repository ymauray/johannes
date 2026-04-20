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
}
