/* SCRIPT INSPECTOR 3
 * version 3.0 Beta 2, August 2014
 * Copyright © 2012-2014, Flipbook Games
 * 
 * Unity's legendary custom inspector for C#, UnityScript and Boo scripts,
 * now transformed into a powerful Script, Shader, and Text Editor!!!
 * 
 * Follow me on http://twitter.com/FlipbookGames
 * Like Flipbook Games on Facebook http://facebook.com/FlipbookGames
 * Join Unity forum discusion http://forum.unity3d.com/threads/138329
 * Contact info@flipbookgames.com for feedback, bug reports, or suggestions.
 * Visit http://flipbookgames.com/ for more info.
 */

using System;
using System.Collections.Generic;
using Debug = UnityEngine.Debug;


namespace ScriptInspector
{
	
public class CsParser : FGParser
{
	protected static string[] tokenLiterals;

	public override string GetToken(int n)
	{
		return tokenLiterals[n];
	}

	public override int TokenToId(string tokenText)
	{
		return Array.BinarySearch<string>(tokenLiterals, tokenText);
	}

	private static readonly string[] csPunctsAndOps = {
		"{", "}", ";", "#", ".", "(", ")", "[", "]", "++", "--", "->", "+", "-",
		"!", "~", "++", "--", "&", "*", "/", "%", "+", "-", "<<", ">>", "<", ">",
		"<=", ">=", "==", "!=", "&", "^", "|", "&&", "||", "??", "?", "::", ":",
		"=", "+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", "=>"
	};

	static CsParser()
	{
		var all = new HashSet<string>(csKeywords);
		all.UnionWith(csTypes);
		all.UnionWith(csPunctsAndOps);
		all.UnionWith(scriptLiterals);
		tokenLiterals = new string[all.Count];
		all.CopyTo(tokenLiterals);
		//Array.Sort<string>(tokenLiterals);
	}

	public void ParseAll(FGTextBuffer.FormatedLine[] lines, string bufferName)
	{
		//Debug.Log("Parsing All of: " + bufferName);
		//var s = new Stopwatch();
		//s.Start();

		var scanner = new CsGrammar.Scanner(CsGrammar.Instance, lines, bufferName);
		//try
		{
			parseTree = CsGrammar.Instance.ParseAll(scanner);
			//	Debug.Log(parseTree);
		}
		//catch (Exception e)
		//{
		//    Debug.LogError("Parsing failed at line: " + scanner.CurrentLine() + ", token " + scanner.CurrentTokenIndex() + " with:\n    " + e + " at " + e.StackTrace);
		//    Debug.Log("Current token: " + scanner.Current.tokenKind + " '" + scanner.Current.text + "'");
		//}

		//for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
		//{
		//	var line = lines[lineIndex];
		//	if (line.tokens == null)
		//		break;

		//	for (var tokenIndex = 0; tokenIndex < line.tokens.Count; tokenIndex++)
		//	{
		//		var t = line.tokens[tokenIndex];
		//		if (t.tokenKind == SyntaxToken.Kind.ContextualKeyword)
		//			t.style = textBuffer.styles.keywordStyle;
		//		if (t.tokenKind == SyntaxToken.Kind.Identifier || t.tokenKind == SyntaxToken.Kind.Keyword ||
		//		    t.tokenKind == SyntaxToken.Kind.ContextualKeyword)
		//		{
		//			var node = t.parent != null ? t.parent.parent : null;
		//			while (node != null)
		//			{
		//				switch (node.RuleName)
		//				{
		//					case "VAR":
		//					case "typeOrGeneric":
		//						t.style = textBuffer.styles.userTypeStyle;
		//						//	node = null;
		//						break;

		//					case "type":
		//						t.style = textBuffer.styles.userTypeStyle;
		//						if (node.parent == null)
		//							node = null;
		//						else
		//						{
		//							//if (node.parent.RuleName == "objectCreationExpression")
		//							//{
		//							//    var typeNode = node.parent.FindChildByName("type", "typeName", "namespaceOrTypeName", "typeOrGeneric", "IDENTIFIER");
		//							//    if (typeNode != null)
		//							//    {
		//							//        //Debug.Log("Found the type node in the objectCreationExpression node at line " + (lineIndex + 1) + ", token " + (tokenIndex + 1));
		//							//        var leaf = typeNode as ParseTree.Leaf;
		//							//        if (leaf != null && leaf.token.text == "Color")
		//							//        {
		//							//    //		Debug.Log("Found a new Color(...) node at line " + (lineIndex + 1) + ", token " + (tokenIndex + 1));
		//							//    //		Debug.Log(node.parent.RuleName + ": " + textBuffer.GetParseTreeNodeSpan(node.parent.parent));
		//							//        }
		//							//    }
		//							//}
		//							if (node.parent.RuleName != "typeArguments")
		//								node = null;
		//						}
		//						break;

		//					case "predefinedType":
		//						t.style = textBuffer.styles.userTypeStyle;
		//						break;

		//					case "parameterModifier":
		//					case "defaultArgument":
		//						node = null;
		//						break;

		//					case "formalParameter":
		//						t.style = textBuffer.styles.parameterStyle;
		//						node = null;
		//						break;

		//					case "typeArguments":
		//						t.style = textBuffer.styles.typeParameterStyle;
		//						node = null;
		//						break;

		//					default:
		//						//	if (node == null && t.style == textBuffer.styles.userTypeStyle)
		//						//		t.style = textBuffer.styles.normalStyle;
		//						break;
		//				}
		//				if (node != null)
		//					node = node.parent;
		//			}
		//		}
		//	}
		//}

		//s.Stop();
		//Debug.Log("Parsing " + System.IO.Path.GetFileName(bufferName) + " (" + lines.Length + " lines) took " + s.ElapsedMilliseconds + " ms.");

		//var sb = new StringBuilder();
		//foreach (var node in scanner.numLookaheads.Keys)
		//    sb.AppendLine(node + ": " + scanner.numLookaheads[node] + " times in " + scanner.timeLookaheads[node] + " ticks (" + scanner.timeLookaheads[node] * 1000 / Stopwatch.Frequency + " ms)");
		//Debug.Log(sb);
	}

	protected override void BuildTopLevelSyntaxTree(string bufferName)
	{
		ParseAll(textBuffer.formatedLines, bufferName);
	//	RunSemanticAnalysis(bufferName);
	}

	protected override void RunSemanticAnalysis(string bufferName)
	{
	//	FGResolver.ScanSymbols(parseTree.root, bufferName);
	//	if (parseTree != null)
	//		FGResolver.ResolveSymbols(parseTree.root, bufferName);
	}

	public override FGGrammar.IScanner MoveAfterLeaf(ParseTree.Leaf leaf)
	{
		var scanner = new CsGrammar.Scanner(CsGrammar.Instance, textBuffer.formatedLines, assetPath);
		return leaf == null ? scanner : scanner.MoveAfterLeaf(leaf) ? scanner : null;
	}

	public override bool ParseLines(int fromLine, int toLineInclusive)
	{
		var formatedLines = textBuffer.formatedLines;

		for (var line = Math.Max(0, fromLine); line <= toLineInclusive; ++line)
		{
			var tokens = formatedLines[line].tokens;
			for (var i = tokens.Count; i --> 0; )
			{
				var t = tokens[i];
				if (t.parent != null)
				{
					t.parent.line = line;
					t.parent.tokenIndex = i;
				}
				/*if (t.tokenKind == SyntaxToken.Kind.ContextualKeyword)
				{
					t.tokenKind = SyntaxToken.Kind.Identifier;
					t.style = textBuffer.styles.normalStyle;
				}
				else*/ if (t.tokenKind == SyntaxToken.Kind.Missing)
				{
					if (t.parent != null && t.parent.parent != null)
						t.parent.parent.syntaxError = null;
					tokens.RemoveAt(i);
				}
			}
		}

		var scanner = new CsGrammar.Scanner(CsGrammar.Instance, formatedLines, assetPath);
		//CsGrammar.Instance.ParseAll(scanner);
		scanner.MoveToLine(fromLine, parseTree);
//        if (scanner.CurrentGrammarNode == null)
//        {
//            if (!scanner.MoveNext())
//                return false;
			
//            FGGrammar.Rule startRule = CsGrammar.Instance.r_compilationUnit;

////			if (parseTree == null)
////			{
////				parseTree = new ParseTree();
////				var rootId = new Id(startRule.GetNt());
////				ids[Start.GetNt()] = startRule;
////			rootId.SetLookahead(this);
////			Start.parent = rootId;
//                scanner.CurrentParseTreeNode = parseTree.root;// = new ParseTree.Node(rootId);
//                scanner.CurrentGrammarNode = startRule;//.Parse(scanner);
			
//                scanner.ErrorParseTreeNode = scanner.CurrentParseTreeNode;
//                scanner.ErrorGrammarNode = scanner.CurrentGrammarNode;
//            //}
//        }

		//Debug.Log("Parsing line " + (fromLine + 1) + " starting from " + scanner.CurrentLine() + ", token " + scanner.CurrentTokenIndex() + " currentToken " + scanner.Current);

		var grammar = CsGrammar.Instance;
		var canContinue = true;
		for (var line = Math.Max(0, scanner.CurrentLine() - 1); canContinue && line <= toLineInclusive; line = scanner.CurrentLine() - 1)
			canContinue = grammar.ParseLine(scanner, line);
			//if (!(canContinue = grammar.ParseLine(scanner, line)))
			//	if (scanner.Current.tokenKind != SyntaxToken.Kind.EOF)
			//		Debug.Log("can't continue at line " + (line + 1) + " token " + scanner.Current);

		if (canContinue && toLineInclusive == formatedLines.Length - 1)
			canContinue = grammar.GetParser.ParseStep(scanner);

		//Debug.Log("canContinue == " + canContinue);

		for (var line = fromLine; line <= toLineInclusive; ++line)
			foreach (var t in formatedLines[line].tokens)
				if (t.tokenKind == SyntaxToken.Kind.ContextualKeyword)
					t.style = t.text == "var" ? textBuffer.styles.userTypeStyle : textBuffer.styles.keywordStyle;

		return canContinue;
		//return true;
	}
	
	public override void FullRefresh()
	{
		base.FullRefresh();
		
		parserThread = new System.Threading.Thread(() =>
		{
			this.OnLoaded();
			this.parserThread = null;
		});
		parserThread.Start();
	}

	public override void LexLine(int currentLine, FGTextBuffer.FormatedLine formatedLine)
	{
		if (parserThread != null)
			parserThread.Join();
		parserThread = null;

		string textLine = textBuffer.lines[currentLine];

		//Stopwatch sw1 = new Stopwatch();
		//Stopwatch sw2 = new Stopwatch();

		//sw2.Start();
		var lineTokens = new List<SyntaxToken>();
		Tokenize(lineTokens, textLine, ref formatedLine.blockState);

//		syntaxTree.SetLineTokens(currentLine, lineTokens);
		formatedLine.tokens = lineTokens;

		if (textLine.Length == 0)
		{
			if (formatedLine.tokens.Count > 0)
				formatedLine.tokens = new List<SyntaxToken>();
		}
		else if (textBuffer.styles != null)
		{
			var lineWidth = textBuffer.CharIndexToColumn(textLine.Length, currentLine);
			if (lineWidth > textBuffer.longestLine)
				textBuffer.longestLine = lineWidth;

			for (var i = 0; i < lineTokens.Count; ++i)
			{
				var token = lineTokens[i];
				switch (token.tokenKind)
				{
					case SyntaxToken.Kind.Whitespace:
					case SyntaxToken.Kind.Missing:
						token.style = textBuffer.styles.normalStyle;
						break;

					case SyntaxToken.Kind.Punctuator:
						token.style = textBuffer.styles.normalStyle;
						break;

					case SyntaxToken.Kind.Keyword:
						if (IsBuiltInLiteral(token.text))
						{
							token.style = textBuffer.styles.constantStyle;
							token.tokenKind = SyntaxToken.Kind.BuiltInLiteral;
						}
						else if (IsBuiltInType(token.text))
						{
							token.style = textBuffer.styles.userTypeStyle;
						}
						else
						{
							token.style = textBuffer.styles.keywordStyle;
						}
						break;

					case SyntaxToken.Kind.Identifier:
						if (IsBuiltInLiteral(token.text))
						{
							token.style = textBuffer.styles.constantStyle;
							token.tokenKind = SyntaxToken.Kind.BuiltInLiteral;
						}
						//else if (IsUnityType(token.text))
						//{
						//	token.style = textBuffer.styles.userTypeStyle;
						//}
						else
						{
							token.style = textBuffer.styles.normalStyle;
						}
						break;

					case SyntaxToken.Kind.IntegerLiteral:
					case SyntaxToken.Kind.RealLiteral:
						token.style = textBuffer.styles.constantStyle;
						break;

					case SyntaxToken.Kind.Comment:
						token.style = textBuffer.styles.commentStyle;
						break;

					case SyntaxToken.Kind.Preprocessor:
						token.style = textBuffer.styles.preprocessorStyle;
						break;

					case SyntaxToken.Kind.CharLiteral:
					case SyntaxToken.Kind.StringLiteral:
					case SyntaxToken.Kind.VerbatimStringBegin:
					case SyntaxToken.Kind.VerbatimStringLiteral:
						token.style = textBuffer.styles.stringStyle;
						break;
				}
				lineTokens[i] = token;
			}
		}
	}

	protected override void Tokenize(List<SyntaxToken> tokens, string line, ref FGTextBuffer.BlockState state)
	{
		int startAt = 0;
		int length = line.Length;
		bool checkPreprocessor = true;
		SyntaxToken token;
		while (startAt < length)
		{
			switch (state)
			{
				case FGTextBuffer.BlockState.None:
					SyntaxToken ws = ScanWhitespace(line, ref startAt);
					if (!string.IsNullOrEmpty(ws.text))
					{
						tokens.Add(ws);
						continue;
					}

					if (checkPreprocessor && line[startAt] == '#')
					{
						tokens.Add(new SyntaxToken(SyntaxToken.Kind.Preprocessor, "#"));
						++startAt;

						ws = ScanWhitespace(line, ref startAt);
						if (!string.IsNullOrEmpty(ws.text))
							tokens.Add(ws);

						token = ScanWord(line, ref startAt);
						token.tokenKind = SyntaxToken.Kind.Preprocessor;
						tokens.Add(token);

						ws = ScanWhitespace(line, ref startAt);
						if (!string.IsNullOrEmpty(ws.text))
							tokens.Add(ws);

						if (startAt < length)
							tokens.Add(new SyntaxToken(SyntaxToken.Kind.Preprocessor, line.Substring(startAt)));

						startAt = length;
						break;
					}
					checkPreprocessor = false;

					if (line[startAt] == '/' && startAt < length - 1)
					{
						if (line[startAt + 1] == '/')
						{
							tokens.Add(new SyntaxToken(SyntaxToken.Kind.Comment, "//"));
							startAt += 2;
							tokens.Add(new SyntaxToken(SyntaxToken.Kind.Comment, line.Substring(startAt)));
							startAt = length;
							break;
						}
						else if (line[startAt + 1] == '*')
						{
							tokens.Add(new SyntaxToken(SyntaxToken.Kind.Comment, "/*"));
							startAt += 2;
							state = FGTextBuffer.BlockState.CommentBlock;
							break;
						}
					}

					if (line[startAt] == '\'')
					{
						token = ScanCharLiteral(line, ref startAt);
						tokens.Add(token);
						break;
					}

					if (line[startAt] == '\"')
					{
						token = ScanStringLiteral(line, ref startAt);
						tokens.Add(token);
						break;
					}

					if (startAt < length - 1 && line[startAt] == '@' && line[startAt + 1] == '\"')
					{
						token = new SyntaxToken(SyntaxToken.Kind.VerbatimStringBegin, line.Substring(startAt, 2));
						tokens.Add(token);
						startAt += 2;
						state = FGTextBuffer.BlockState.StringBlock;
						break;
					}

					if (line[startAt] >= '0' && line[startAt] <= '9'
					    || startAt < length - 1 && line[startAt] == '.' && line[startAt + 1] >= '0' && line[startAt + 1] <= '9')
					{
						token = ScanNumericLiteral(line, ref startAt);
						tokens.Add(token);
						break;
					}

					token = ScanIdentifierOrKeyword(line, ref startAt);
					if (!token.IsMissing())
					{
						tokens.Add(token);
						break;
					}

					// Multi-character operators / punctuators
					// "++", "--", "<<", ">>", "<=", ">=", "==", "!=", "&&", "||", "??", "+=", "-=", "*=", "/=", "%=",
					// "&=", "|=", "^=", "<<=", ">>=", "=>", "::"
					var punctuatorStart = startAt++;
					if (startAt < line.Length)
					{
						switch (line[punctuatorStart])
						{
							case '?':
								if (line[startAt] == '?')
									++startAt;
								break;
							case '+':
								if (line[startAt] == '+' || line[startAt] == '=')
									++startAt;
								break;
							case '-':
								if (line[startAt] == '-' || line[startAt] == '=')
									++startAt;
								break;
							case '<':
								if (line[startAt] == '=')
									++startAt;
								else if (line[startAt] == '<')
								{
									++startAt;
									if (startAt < line.Length && line[startAt] == '=')
										++startAt;
								}
								break;
							case '>':
								if (line[startAt] == '=')
									++startAt;
								//else if (startAt < line.Length && line[startAt] == '>')
								//{
								//    ++startAt;
								//    if (line[startAt] == '=')
								//        ++startAt;
								//}
								break;
							case '=':
								if (line[startAt] == '=' || line[startAt] == '>')
									++startAt;
								break;
							case '&':
								if (line[startAt] == '=' || line[startAt] == '&')
									++startAt;
								break;
							case '|':
								if (line[startAt] == '=' || line[startAt] == '|')
									++startAt;
								break;
							case '*':
							case '/':
							case '%':
							case '^':
							case '!':
								if (line[startAt] == '=')
									++startAt;
								break;
							case ':':
								if (line[startAt] == ':')
									++startAt;
								break;
						}
					}
					tokens.Add(new SyntaxToken(SyntaxToken.Kind.Punctuator, line.Substring(punctuatorStart, startAt - punctuatorStart)));
					break;

				case FGTextBuffer.BlockState.CommentBlock:
					int commentBlockEnd = line.IndexOf("*/", startAt);
					if (commentBlockEnd == -1)
					{
						tokens.Add(new SyntaxToken(SyntaxToken.Kind.Comment, line.Substring(startAt)));
						startAt = length;
					}
					else
					{
						tokens.Add(new SyntaxToken(SyntaxToken.Kind.Comment, line.Substring(startAt, commentBlockEnd + 2 - startAt)));
						startAt = commentBlockEnd + 2;
						state = FGTextBuffer.BlockState.None;
					}
					break;

				case FGTextBuffer.BlockState.StringBlock:
					int i = startAt;
					int closingQuote = line.IndexOf('\"', startAt);
					while (closingQuote != -1 && closingQuote < length - 1 && line[closingQuote + 1] == '\"')
					{
						i = closingQuote + 2;
						closingQuote = line.IndexOf('\"', i);
					}
					if (closingQuote == -1)
					{
						tokens.Add(new SyntaxToken(SyntaxToken.Kind.VerbatimStringLiteral, line.Substring(startAt)));
						startAt = length;
					}
					else
					{
						tokens.Add(new SyntaxToken(SyntaxToken.Kind.VerbatimStringLiteral, line.Substring(startAt, closingQuote - startAt)));
						startAt = closingQuote;
						tokens.Add(new SyntaxToken(SyntaxToken.Kind.VerbatimStringLiteral, line.Substring(startAt, 1)));
						++startAt;
						state = FGTextBuffer.BlockState.None;
					}
					break;
			}
		}
	}

	private new SyntaxToken ScanIdentifierOrKeyword(string line, ref int startAt)
	{
		var token = FGParser.ScanIdentifierOrKeyword(line, ref startAt);
		if (token.tokenKind == SyntaxToken.Kind.Keyword && !IsKeyword(token.text) && !IsBuiltInType(token.text))
			token.tokenKind = SyntaxToken.Kind.Identifier;
		return token;
	}
}

}
