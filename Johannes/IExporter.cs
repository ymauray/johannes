using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Johannes
{
	public interface IExporter
	{
		void Paragraph(string styleId, List<ParagraphRun> runs);
		void FinishExport();
	}

	public abstract partial class AbstractExporter : IExporter
	{
		public abstract void Paragraph(string styleId, List<ParagraphRun> runs);
		public abstract void FinishExport();

		[GeneratedRegex(@"[ \u00A0~]\?")]
		protected static partial Regex SpaceBeforeQuestionMarkRegex();

		[GeneratedRegex(@"\?[ \u00A0~]")]
		protected static partial Regex SpaceAfterQuestionMarkRegex();

		[GeneratedRegex(@"[ \u00A0~]\!")]
		protected static partial Regex SpaceBeforeExclamationMarkRegex();

		[GeneratedRegex(@"\![ \u00A0~]")]
		protected static partial Regex SpaceAfterExclamationMarkRegex();

		[GeneratedRegex(@"[ \u00A0~]:")]
		protected static partial Regex ColonRegex();

		[GeneratedRegex(@"[ \u00A0~];")]
		protected static partial Regex SemiColonRegex();
	}
}
