using System;
using System.Collections.Generic;
using System.Text;

namespace Johannes
{
	public partial class EpubExporter : IExporter
	{
		public EpubExporter(FileInfo epubDir)
		{
		}

		public void FinishExport()
		{
			// Keep empty for now, will be implemented later.
		}

		public void Paragraph(string styleId, List<ParagraphRun> runs)
		{
			// Keep empty for now, will be implemented later.
		}
	}
}
