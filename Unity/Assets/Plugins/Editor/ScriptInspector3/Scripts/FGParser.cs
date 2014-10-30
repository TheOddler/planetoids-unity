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

using System.IO;
using System.Threading;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using FormatedLine = FGTextBuffer.FormatedLine;
using Debug = UnityEngine.Debug;


namespace ScriptInspector
{

public struct TextPosition
{
	public int line;
	public int index;

	public TextPosition(int line, int index)
	{
		this.line = line;
		this.index = index;
	}

	public static TextPosition operator + (TextPosition other, int offset)
	{
		return new TextPosition { line = other.line, index = other.index + offset };
	}

	public static bool operator < (TextPosition lhs, TextPosition rhs)
	{
		return lhs.line < rhs.line || lhs.line == rhs.line && lhs.index < rhs.index;
	}

	public static bool operator > (TextPosition lhs, TextPosition rhs)
	{
		return lhs.line > rhs.line || lhs.line == rhs.line && lhs.index > rhs.index;
	}

	public bool Move(FGTextBuffer textBuffer, int offset)
	{
		while (offset > 0)
		{
			var lineTokensCount = textBuffer.formatedLines[line].tokens.Count;
			if (lineTokensCount >= index + offset)
			{
				index += offset;
				return true;
			}
			if (line < textBuffer.formatedLines.Length - 1)
			{
				offset -= lineTokensCount - index;
				++line;
				index = 0;
			}
			else
			{
				index = lineTokensCount;
				return false;
			}
		}

		while (offset < 0)
		{
			if (index + offset >= 0)
			{
				index += offset;
				return true;
			}
			if (line > 0)
			{
				offset += index;
				--line;
				index = textBuffer.formatedLines[line].tokens.Count;
			}
			else
			{
				index = 0;
				return false;
			}
		}

		return true;
	}

	public override string ToString()
	{
		return "TextPosition (line: " + line + ", index: " + index + ")";
	}
}

public struct TextOffset
{
	public int lines;
	public int indexOffset;
}

public struct TextSpan
{
	public int line;
	public int index;
	public int lineOffset;
	public int indexOffset;

	public override string ToString()
	{
		return "TextSpan{ line = " + (line+1) + ", fromChar = " + index + ", lineOffset = " + lineOffset + ", toChar = " + indexOffset + " }";
	}

	public static TextSpan CreateEmpty(TextPosition position)
	{
		return new TextSpan { line = position.line, index = position.index };
	}

	public static TextSpan Create(TextPosition from, TextPosition to)
	{
		return new TextSpan
		{
			line = from.line,
			index = from.index,
			lineOffset = to.line - from.line,
			indexOffset = to.index - (to.line == from.line ? from.index : 0)
		};
	}

	public static TextSpan CreateBetween(TextSpan from, TextSpan to)
	{
		return Create(from.EndPosition, to.StartPosition);
	}

	public static TextSpan CreateEnclosing(TextSpan from, TextSpan to)
	{
		return Create(from.StartPosition, to.EndPosition);
	}

	public static TextSpan Create(TextPosition start, TextOffset length)
	{
		return new TextSpan
		{
			line = start.line,
			index = start.index,
			lineOffset = length.lines,
			indexOffset = length.indexOffset
		};
	}

	public TextPosition StartPosition
	{
		get { return new TextPosition { line = line, index = index }; }
		set
		{
			if (value.line == line + lineOffset)
			{
				line = value.line;
				lineOffset = 0;
				indexOffset = index + indexOffset - value.index;
				index = value.index;
			}
			else
			{
				lineOffset = line + lineOffset - value.line;
				line = value.line;
				index = value.index;
			}
		}
	}

	public TextPosition EndPosition
	{
		get { return new TextPosition { line = line + lineOffset, index = indexOffset + (lineOffset == 0 ? index : 0) }; }
		set
		{
			if (value.line == line)
			{
				lineOffset = 0;
				indexOffset = value.index - index;
			}
			else
			{
				lineOffset = value.line - line;
				indexOffset = value.index;
			}
		}
	}

	public void Offset(int deltaLines, int deltaIndex)
	{
		line += deltaLines;
		index += deltaIndex;
	}

	public bool Contains(TextPosition position)
	{
		return !(position.line < line
			|| position.line == line && (position.index < index || lineOffset == 0 && position.index > index + indexOffset)
			|| position.line > line + lineOffset
			|| position.line == line + lineOffset && position.index > indexOffset);
	}
}

public class SyntaxToken //: IComparable<SyntaxToken>
{
	public enum Kind
	{
		Missing,
		Whitespace,
		Comment,
		Preprocessor,
		VerbatimStringLiteral,
		LastWSToken, // Marker only
		VerbatimStringBegin,
		BuiltInLiteral,
		CharLiteral,
		StringLiteral,
		IntegerLiteral,
		RealLiteral,
		Punctuator,
		Keyword,
		Identifier,
		ContextualKeyword,
		EOF,
	}

	public Kind tokenKind;
	public GUIStyle style;
	public ParseTree.Leaf parent;
	//public TextSpan textSpan;
	public string text;
	public int tokenId;

	public static SyntaxToken CreateMissing()
	{
		return new SyntaxToken(Kind.Missing, string.Empty) { parent = null };
	}

	public SyntaxToken(Kind kind, string text)
	{
		parent = null;
		tokenKind = kind;
		this.text = text;
		tokenId = -1;
		style = null;
	}

	public bool IsMissing()
	{
		return tokenKind == Kind.Missing;
	}

	//public SymbolDefinition GetResolvedSymbol()
	//{
	//    for (ParseTree.BaseNode p = parent; p != null; p = p.parent)
	//    {
	//        if (p.resolvedSymbol != null)
	//        {
	//            if (p.resolvedSymbol is MethodGroupDefinition)
	//                for (ParseTree.BaseNode pp = p.parent; pp != null; pp = pp.parent)
	//                    if (pp.resolvedSymbol is MethodDefinition)
	//                        return pp.resolvedSymbol;
	//            return p.resolvedSymbol;
	//        }
	//    }
	//    return null;
	//}

	public override string ToString() { return tokenKind +"(\"" + text + "\")"; }

	public string Dump() { return "[Token: " + tokenKind + " \"" + text + "\"]"; }

//	public int CompareTo(SyntaxToken other)
//	{
//		var t = tokenKind.GetHashCode().CompareTo(tokenKind.GetHashCode());
//		return t != 0 ? t : text.CompareTo(other.text);
//	}
}

public abstract class FGParser
{
	protected static readonly char[] whitespaces = { ' ', '\t' };
	//protected static readonly Regex emailRegex = new Regex(@"\b([A-Z0-9._%-]+)@([A-Z0-9.-]+\.[A-Z]{2,6})\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Dictionary<string, Type> parserTypes = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

	public static FGParser Create(FGTextBuffer textBuffer, string path)
	{
		if (parserTypes.Count == 0)
			RegisterParsers();

		Type parserType;
		FGParser parser;
		var extension = Path.GetExtension(path) ?? String.Empty;
		if (parserTypes.TryGetValue(extension, out parserType))
			parser = (FGParser) Activator.CreateInstance(parserType);
		else
			parser = new TextParser();
		
		parser.textBuffer = textBuffer;
		parser.assetPath = path;
		return parser;
	}

	private static void RegisterParsers()
	{
		parserTypes.Add(".cs", typeof(CsParser));
		parserTypes.Add(".js", typeof(JsParser));
		parserTypes.Add(".boo", typeof(BooParser));
		
		parserTypes.Add(".shader", typeof(ShaderParser));
		parserTypes.Add(".cg", typeof(ShaderParser));
		parserTypes.Add(".cginc", typeof(ShaderParser));
		
		parserTypes.Add(".txt", typeof(TextParser));
	}

	static FGParser()
	{
		var typeNames = new HashSet<string>();
		var assemblies = AppDomain.CurrentDomain.GetAssemblies();
		foreach (var assembly in assemblies)
		{
			try
			{
				if (assembly is System.Reflection.Emit.AssemblyBuilder)
					continue;
				
				var takeAllTypes = AssemblyDefinition.IsScriptAssemblyName(assembly.GetName().Name);
				var assemblyTypes = takeAllTypes ? assembly.GetTypes() : assembly.GetExportedTypes();
				foreach (var type in assemblyTypes)
				{
					var name = type.Name;
					var index = name.IndexOf('`');
					if (index >= 0)
						name = name.Remove(index);
					typeNames.Add(name);
					if (type.IsSubclassOf(typeof(Attribute)) && name.EndsWith("Attribute", StringComparison.OrdinalIgnoreCase))
						typeNames.Add(type.Name.Substring(0, type.Name.Length - "Attribute".Length));
				}
			}
			catch (ReflectionTypeLoadException)
			{
				Debug.LogWarning("Error reading types from assembly " + assembly.FullName);
			}
		}
		unityTypes = typeNames.ToArray();

		Array.Sort(unityTypes);
		Array.Sort(jsKeywords);
		Array.Sort(booKeywords);
	}


	// Instance members

	protected string assetPath;

	protected FGTextBuffer textBuffer;
	public ParseTree parseTree { get; protected set; }

	public void OnLoaded()
	{
		BuildTopLevelSyntaxTree(assetPath);
	}

	public virtual FGGrammar.IScanner MoveAfterLeaf(ParseTree.Leaf leaf)
	{
		return null;
	}

	public virtual bool ParseLines(int fromLine, int toLineInclusive)
	{
		return true;
	}

	protected Thread parserThread;
	
	public virtual void FullRefresh()
	{
		if (parserThread != null)
			parserThread.Join();
		parserThread = null;
	}

	public virtual void LexLine(int currentLine, FormatedLine formatedLine)
	{
		if (parserThread != null)
			parserThread.Join();
		parserThread = null;

		string textLine = textBuffer.lines[currentLine];
		var lineTokens = new List<SyntaxToken>();

		if (textLine.Length == 0)
		{
			formatedLine.tokens = lineTokens;
		}
		else
		{
			//Tokenize(lineTokens, textLine, ref formatedLine.blockState);
			lineTokens.Add(new SyntaxToken(SyntaxToken.Kind.Comment, textLine) { style = textBuffer.styles.normalStyle });

			formatedLine.tokens = lineTokens;

			var lineWidth = textBuffer.CharIndexToColumn(textLine.Length, currentLine);
			if (lineWidth > textBuffer.longestLine)
				textBuffer.longestLine = lineWidth;
		}
	}
	
	protected virtual void Tokenize(List<SyntaxToken> tokens, string line, ref FGTextBuffer.BlockState state)	
	{
	}
	
	protected virtual void BuildTopLevelSyntaxTree(string bufferName)
	{
	}

	protected virtual void RunSemanticAnalysis(string bufferName)
	{
	}

	public virtual void CutParseTree(int fromLine, FormatedLine[] formatedLines)
	{
	//	Debug.Log("Cutting tree. Line: " + (fromLine + 1));//scanner.CurrentLine());
	//	Debug.Log(scanner.Current.parent + "\n" + scanner.CurrentParseTreeNode);;// + "\n\n" + scanner.CurrentGrammarNode.parent.parent.parent.parent);
		if (parseTree == null)
			return;

		ParseTree.BaseNode cut = null;//scanner.Current.parent;
		var prevLine = fromLine;
		while (cut == null && prevLine --> 0)
		{
			var tokens = textBuffer.formatedLines[prevLine].tokens;
			for (var i = tokens.Count; i --> 0; )
				if (tokens[i].tokenKind > SyntaxToken.Kind.LastWSToken && tokens[i].parent != null && tokens[i].parent.syntaxError == null)
				{
					cut = tokens[i].parent;
					break;
				}
		}

		var cutThis = false;
		if (cut == null)
		{
			cut = parseTree.root.ChildAt(0);
			cutThis = true;
		}

		while (cut != null)
		{
			var cutParent = cut.parent;
			if (cutParent == null)
				break;
			var cutIndex = cutThis ? cut.childIndex : cut.childIndex + 1;
			while (cutIndex > 0)
			{
				var child = cutParent.ChildAt(cutIndex - 1);
				if (child != null && !child.HasLeafs())
					--cutIndex;
				else
					break;
			}
			cutThis = cutThis && cutIndex == 0;
			if (cutIndex < cutParent.numValidNodes)
			{
				cutParent.InvalidateFrom(cutIndex);
			}
			cut = cutParent;
			cut.syntaxError = null;
		}

		//Debug.Log("Cut " + numCut + " nodes... " + scanner.Current);

		//Debug.Log(scanner.Current + "\n" + scanner.CurrentParseTreeNode + "\n\n" + scanner.CurrentGrammarNode);//.parent.parent.parent.parent.parent.parent.parent.parent.parent.parent);

		/*
		for (var i = Math.Max(0, fromLine); i < formatedLines.Length; ++i)
			if (formatedLines[i].tokens != null)
				for (int j = 0; j < formatedLines[i].tokens.Count; j++)
				{
					var t = formatedLines[i].tokens[j];
					if (t.tokenKind == SyntaxToken.Kind.Missing)
					{
						formatedLines[i].tokens.RemoveAt(j);
					}
					else if (t.parent != null)
					{
						t.parent.resolvedSymbol = null;
						t.parent.RemoveToken();
					}
				}
		//*/
		//	FGResolver.GlobalNamespace = null;
	}

	//public virtual void LexLine(int currentLine, FormatedLine formatedLine)
	//{
	//	throw new NotImplementedException();

		//string line = FGTextBuffer.ExpandTabs(textBuffer.lines[currentLine]);
		//if (line.Length == 0)
		//{
		//    //if (formatedLine.textBlocks == null || formatedLine.textBlocks.Length > 0)
		//    //    formatedLine.textBlocks = new TextBlock[] { new TextBlock(string.Empty, textBuffer.styles.normalStyle) };
		//    if (formatedLine.tokens == null || formatedLine.tokens.Count > 0)
		//        formatedLine.tokens = new List<SyntaxToken>();
		//    return;
		//}

		//if (line.Length > textBuffer.longestLine)
		//    textBuffer.longestLine = line.Length;

		//List<TextBlock> blocks = new List<TextBlock>();

		//if (textBuffer.isText)
		//{
		//    PushComment(ref blocks, line, textBuffer.styles.normalStyle);
		//    formatedLine.textBlocks = blocks.ToArray();
		//    return;
		//}

		//bool checkPreprocessor = true;
		//int startIndex = 0;
		//while (startIndex < line.Length)
		//{
		//    int index;

		//    if (formatedLine.blockState == BlockState.CommentBlock)
		//    {
		//        index = line.IndexOf("*/", startIndex);
		//        if (index == -1)
		//        {
		//            PushComment(ref blocks, line.Substring(startIndex));
		//            break;
		//        }
		//        else
		//        {
		//            PushComment(ref blocks, line.Substring(startIndex, index - startIndex + 2));
		//            startIndex = index + 2;
		//            formatedLine.blockState = BlockState.None;
		//            continue;
		//        }
		//    }
		//    else if (formatedLine.blockState == BlockState.StringBlock)
		//    {
		//        //int firstIndex = IndexOf2(line, startIndex, '\\', '\"');
		//        index = line.IndexOf("\"\"\"", startIndex);
		//        if (index == -1)
		//        {
		//            blocks.Add(new TextBlock(line.Substring(startIndex), textBuffer.styles.stringStyle));
		//            break;
		//        }
		//        else
		//        {
		//            blocks.Add(new TextBlock(line.Substring(startIndex, index - startIndex + 3), textBuffer.styles.stringStyle));
		//            startIndex = index + 3;
		//            formatedLine.blockState = BlockState.None;
		//            continue;
		//        }
		//    }

		//    if (textBuffer.isBooFile)
		//        index = IndexOf5(line, startIndex, "\"", "'", "#", "//", "/*");
		//    else if (!checkPreprocessor)
		//        index = IndexOf6(line, startIndex, "\"", "'", "#", "@\"", "//", "/*");
		//    else
		//        index = IndexOf5(line, startIndex, "\"", "'", "@\"", "//", "/*");
		//    if (index == -1)
		//        index = line.Length;

		//    if (index > 0)
		//    {
		//        string directive = PushCode(ref blocks, line.Substring(startIndex, index - startIndex), startIndex == 0
		//            || checkPreprocessor && line.Substring(0, startIndex).Trim(whitespaces) == string.Empty);
		//        if (directive != null)
		//        {
		//            index = line.IndexOf(directive) + directive.Length;
		//            if (index == line.Length)
		//                break;

		//            int indexLineComment = directive.Trim(whitespaces) == "#region" ? -1 : line.IndexOf("//", index);
		//            if (indexLineComment != -1)
		//            {
		//                blocks.Add(new TextBlock(line.Substring(index, indexLineComment - index), textBuffer.styles.normalStyle));
		//                PushComment(ref blocks, line.Substring(indexLineComment));
		//            }
		//            else
		//            {
		//                blocks.Add(new TextBlock(line.Substring(index), textBuffer.styles.normalStyle));
		//            }
		//            break;
		//        }
		//        else
		//            checkPreprocessor = false;
		//    }

		//    startIndex = index;

		//    if (index < line.Length)
		//    {
		//        if (line[index] == '@')
		//            ++index;
		//        else if (textBuffer.isBooFile && index < line.Length - 2 && line.Substring(index, 3) == "\"\"\"")
		//        {
		//            // String block starting with """
		//            blocks.Add(new TextBlock("\"\"\"", textBuffer.styles.stringStyle));
		//            startIndex += 3;
		//            formatedLine.blockState = BlockState.StringBlock;
		//            continue;
		//        }

		//        char terminalChar = line[index];
		//        if (terminalChar == '\"' || terminalChar == '\'')
		//        {
		//            // String, Char, or RegExp literal
		//            for (++index; index < line.Length; )
		//            {
		//                index = IndexOf2(line, index, terminalChar, '\\');
		//                if (index == -1)
		//                {
		//                    index = line.Length;
		//                    break;
		//                }
		//                else if (line[index] == '\\')
		//                {
		//                    ++index;
		//                    if (index == line.Length)
		//                        break;
		//                    ++index;
		//                }
		//                else
		//                {
		//                    ++index;
		//                    break;
		//                }
		//            };

		//            blocks.Add(new TextBlock(line.Substring(startIndex, index - startIndex), textBuffer.styles.stringStyle));
		//            startIndex = index;
		//        }
		//        else if (line[index] == '#' || line[index + 1] == '/')
		//        {
		//            // Comment till end of line
		//            PushComment(ref blocks, line.Substring(index));
		//            break;
		//        }
		//        else
		//        {
		//            // Comment block starting with /*
		//            PushComment(ref blocks, line.Substring(index, 2));
		//            startIndex += 2;
		//            formatedLine.blockState = BlockState.CommentBlock;
		//        }
		//    }
		//}

		//formatedLine.textBlocks = blocks.ToArray();
	//}

/*
	private void PushComment(ref List<TextBlock> blocks, string line, GUIStyle commentStyle = null)
	{
		string address;
		int index;

		if (commentStyle == null)
			commentStyle = textBuffer.styles.commentStyle;

		for (int startAt = 0; startAt < line.Length; )
		{
			int hyperlink = IndexOf3(line, startAt, "http://", "https://", "ftp://");
			if (hyperlink == -1)
				hyperlink = line.Length;

			while (hyperlink != startAt)
			{
				Match emailMatch = emailRegex.Match(line, startAt, hyperlink - startAt);
				if (emailMatch.Success)
				{
					if (emailMatch.Index > startAt)
						blocks.Add(new TextBlock(line.Substring(startAt, emailMatch.Index - startAt), commentStyle));

					address = line.Substring(emailMatch.Index, emailMatch.Length);
					blocks.Add(new TextBlock(address, textBuffer.styles.mailtoStyle));
					address = "mailto:" + address;
					if (textBuffer.IsLoading)
					{
						index = Array.BinarySearch<string>(textBuffer.hyperlinks, address, StringComparer.OrdinalIgnoreCase);
						if (index < 0)
							ArrayUtility.Insert(ref textBuffer.hyperlinks, -1 - index, address);
					}

					startAt = emailMatch.Index + emailMatch.Length;
					continue;
				}

				blocks.Add(new TextBlock(line.Substring(startAt, hyperlink - startAt), commentStyle));
				startAt = hyperlink;
			}

			if (startAt == line.Length)
				break;

			int i = line.IndexOf(':', startAt) + 3;
			while (i < line.Length)
			{
				char c = line[i];
				if (c >= 'A' && c <= 'Z' || c >= 'a' && c <= 'z' || c >= '0' && c <= '9' || c == '_' || c == '.' ||
					c == '-' || c == '=' || c == '+' || c == '%' || c == '&' || c == '?' || c == '/' || c == '#')
					++i;
				else
					break;
			}

			address = line.Substring(startAt, i - startAt);
			blocks.Add(new TextBlock(address, textBuffer.styles.hyperlinkStyle));
			if (textBuffer.IsLoading)
			{
				index = Array.BinarySearch<string>(textBuffer.hyperlinks, address, StringComparer.OrdinalIgnoreCase);
				if (index < 0)
					ArrayUtility.Insert(ref textBuffer.hyperlinks, -1 - index, address);
			}

			startAt = i;
		}
	}
*/

/*
	private string PushCode(ref List<TextBlock> blocks, string line, bool checkPreprocessor)
	{
		int startAt = 0;
		while (startAt < line.Length)
		{
			char c = line[startAt];
			if (c >= 'A' && c <= 'Z' || c >= 'a' && c <= 'z' || c == '_' || checkPreprocessor && c == '#')
			{
				int i = startAt + 1;
				for (; i < line.Length; ++i)
				{
					c = line[i];
					if (!(c >= '0' && c <= '9' || c >= 'A' && c <= 'Z' || c >= 'a' && c <= 'z' || c == '_'))
						break;
				}
				string word = line.Substring(startAt, i - startAt);

				if (checkPreprocessor && IsPreprocessorDirective(word))
				{
					blocks.Add(new TextBlock(word, textBuffer.styles.preprocessorStyle));
					return word;
				}
				checkPreprocessor = false;

				if (IsKeyword(word))
					blocks.Add(new TextBlock(word, textBuffer.styles.keywordStyle));
				else if (IsBuiltInLiteral(word))
					blocks.Add(new TextBlock(word, textBuffer.styles.constantStyle));
				else if (IsBuiltInType(word) || IsUnityType(word))
					blocks.Add(new TextBlock(word, textBuffer.styles.userTypeStyle));
				else
					blocks.Add(new TextBlock(word, textBuffer.styles.normalStyle));
				startAt = i;
			}
			else
			{
				int i = startAt + 1;
				for (; i < line.Length; ++i)
				{
					c = line[i];
					if (c >= 'A' && c <= 'Z' || c >= 'a' && c <= 'z' || c == '_' || c == '#')
						break;
				}
				blocks.Add(new TextBlock(line.Substring(startAt, i - startAt), textBuffer.styles.normalStyle));
				startAt = i;

				checkPreprocessor = checkPreprocessor && line.Substring(0, i).Trim(whitespaces) == string.Empty;
			}
		}

		return null;
	}
*/

	public string[] PreprocessorDirectives { get { return textBuffer.isShader ? shaderPreprocessor : preprocessor; } }
	public string[] Keywords { get { return textBuffer.isCsFile ? csKeywords : textBuffer.isJsFile ? jsKeywords : textBuffer.isBooFile ? booKeywords : textBuffer.isShader ? shaderKeywords : new string[0]; } }
	public string[] BuiltInLiterals { get { return textBuffer.isShader ? shaderLiterals : scriptLiterals; } }
	public string[] BuiltInTypes { get { return textBuffer.isCsFile ? csTypes : textBuffer.isJsFile ? jsTypes : textBuffer.isBooFile ? booTypes : textBuffer.isShader ? shaderTypes : new string[0]; } }
	//public string[] BuiltInConstants { get { return textBuffer.isCsFile ? csTypes : textBuffer.isJsFile ? jsTypes : textBuffer.isBooFile ? booTypes : new string[0]; } }

	#region protected static definitions

	protected static readonly string[] preprocessor = new string[] {
		"#define", "#elif", "#else", "#endif", "#endregion", "#error", "#if", "#line", "#pragma", "#region", "#undef", "#warning"
	};

	protected static readonly string[] shaderPreprocessor = new string[] {
		"#define", "#elif", "#else", "#endif", "#error", "#if", "#ifdef", "#ifndef", "#include", "#pragma", "#undef",
		"CGINCLUDE", "CGPROGRAM", "ENDCG", "GLSLEND", "GLSLPROGRAM"
	};

	protected static readonly string[] scriptLiterals = new string[] {
		"false", "null", "true",
	};

	protected static readonly string[] csKeywords = new string[] {
		"abstract", "as", "base", "break", "case", "catch", "checked", "class", "const", "continue",
		"default", "delegate", "do", "else", "enum", "event", "explicit", "extern", "finally",
		"fixed", "for", "foreach", "goto", "if", "implicit", "in", "interface", "internal", "is",
		"lock", "namespace", "new", "operator", "out", "override", "params", "partial", "private",
		"protected", "public", "readonly", "ref", "return", "sealed", "sizeof", "stackalloc", "static",
		"struct", "switch", "this", "throw", "try", "typeof", "unchecked", "unsafe", "using", "virtual",
		"volatile", "while"
	};

	protected static readonly string[] csTypes = new string[] {
		"bool", "byte", "char", "decimal", "double", "float", "int", "long", "object", "sbyte", "short",
		"string", "uint", "ulong", "ushort", "void"
	};

	protected static readonly string[] jsKeywords = new string[] {
		"abstract", "else", "instanceof", "super", "enum", "switch", "break", "static", "export",
		"interface", "synchronized", "extends", "let", "this", "case", "with", "throw",
		"catch", "final", "native", "throws", "finally", "new", "transient", "class",
		"const", "for", "package", "try", "continue", "private", "typeof", "debugger", "goto",
		"protected", "default", "if", "public", "delete", "implements", "return", "volatile", "do",
		"import", "while", "in", "function"
	};

	protected static readonly string[] jsTypes = new string[] {
		"boolean", "byte", "char", "double", "float", "int", "long", "short", "var", "void"
	};

	protected static readonly string[] booKeywords = new string[] {
		"abstract", "and", "as", "break", "callable", "cast", "class", "const", "constructor", "destructor", "continue",
		"def", "do", "elif", "else", "enum", "ensure", "event", "except", "final", "for", "from", "given", "get", "goto",
		"if", "interface", "in", "include", "import", "is", "isa", "mixin", "namespace", "not", "or", "otherwise",
		"override", "pass", "raise", "retry", "self", "struct", "return", "set", "success", "try", "transient", "virtual",
		"while", "when", "unless", "yield", 

		"public", "protected", "private", "internal", "static", 

		// builtin
		"len", "__addressof__", "__eval__", "__switch__", "array", "matrix", "typeof", "assert", "print", "gets", "prompt", 
		"enumerate", "zip", "filter", "map",
	};

	protected static readonly string[] booTypes = new string[] {
		"bool", "byte", "char", "date", "decimal", "double", "int", "long", "object", "sbyte", "short", "single", "string",
		"timespan", "uint", "ulong", "ushort", "void"
	};

	protected static readonly string[] shaderKeywords = new string[] {
		"AlphaTest", "Ambient", "Bind", "Blend", "BorderScale", "ColorMask", "ColorMaterial", "Combine", "ConstantColor",
		"Cull", "Density", "Diffuse", "Emission", "Fallback", "Fog", "Lerp", "Lighting", "LightmapMode", "LightMode",
		"LightTexCount", "Material", "Matrix", "Mode", "Name", "Offset", "RequireOptions", "SeparateSpecular", "SetTexture",
		"Shininess", "Specular", "TexGen", "TextureScale", "TextureSize", "UsePass", "ZTest", "ZWrite",
	};

	protected static readonly string[] shaderLiterals = new string[] {
		"A", "Always", "AmbientAndDiffuse", "AppDstAdd", "AppSrcAdd", "Back", "CubeNormal", "CubeReflect", "DstAlpha",
		"DstColor", "Emission", "EyeLinear", "Exp", "Exp2", "Front", "GEqual", "Greater", "LEqual", "Less", "Linear",
		"None", "Normal", "NotEqual", "ObjectLinear", "Off", "On", "One", "OneMinusDstAlpha", "OneMinusDstColor",
		"OneMinusSrcAlpha", "OneMinusSrcColor", "Pixel", "PixelOnly", "PixelOrNone", "RGB", "SoftVegetation", "SrcAlpha",
		"SrcColor", "SphereMap", "Vertex", "VertexAndPixel", "VertexOnly", "VertexOrNone", "VertexOrPixel", "Tangent",
		"Texcoord", "Texcoord0", "Texcoord1", "Zero",
	};

	protected static readonly string[] shaderTypes = new string[] {
		"2D", "BindChannels", "Category", "Color", "Constant", "Cube", "Float", "Fog", "GrabPass", "Pass", "Previous",
		"Properties", "Range", "Rect", "Shader", "SubShader", "Tags", "Texture", "Vector", "_CosTime", "_CubeNormalize",
		"_Light2World", "_ModelLightColor", "_Object2Light", "_Object2World", "_ObjectSpaceCameraPos", "_ObjectSpaceLightPos",
		"_ProjectionParams", "_SinTime", "_SpecFalloff", "_SpecularLightColor", "_Time", "_World2Light", "_World2Object",
	};

	protected static string[] unityTypes = null; /*new string[] /*{
		// Runtime classes
		"ADBannerView", "ADError", "ADInterstitialAd", "AccelerationEvent", "ActionScript", "AndroidInput",
		"AndroidJNIHelper", "AndroidJNI", "AndroidJavaObject", "AndroidJavaClass", "AnimationCurve", "AnimationEvent",
		"AnimationState", "Application", "Array", "AudioSettings", "BitStream", "BoneWeight", "Bounds", "Caching",
		"ClothSkinningCoefficient", "Collision", "Color32", "Color", "CombineInstance", "Compass", "ContactPoint",
		"ControllerColliderHit", "Debug", "DetailPrototype", "Event", "GL", "GUIContent", "GUILayoutOption",
		"GUILayoutUtility", "GUILayout", "GUISettings", "GUIStyleState", "GUIStyle", "GUIUtility", "GUI", "GeometryUtility",
		"Gizmos", "Graphics", "Gyroscope", "Handheld", "Hashtable", "HostData", "IAchievementDescription", "IAchievement",
		"ILeaderboard", "IScore", "ISocialPlatform", "GameCenterPlatform", "IUserProfile", "ILocalUser", "Input",
		"JointDrive", "JointLimits", "JointMotor", "JointSpring", "Keyframe", "LayerMask", "LightmapData", "LightmapSettings",
		"LocalNotification", "LocationInfo", "LocationService", "MasterServer", "MaterialPropertyBlock", "Mathf", "Matrix4x4",
		"Microphone", "NavMeshHit", "NavMeshPath", "NetworkMessageInfo", "NetworkPlayer", "NetworkViewID", "Network",
		"NotificationServices", "Object", "AnimationClip", "AssetBundle", "AudioClip", "Component", "Behaviour", "Animation",
		"AudioChorusFilter", "AudioDistortionFilter", "AudioEchoFilter", "AudioHighPassFilter", "AudioListener",
		"AudioLowPassFilter", "AudioReverbFilter", "AudioReverbZone", "AudioSource", "Camera", "ConstantForce", "GUIElement",
		"GUIText", "GUITexture", "GUILayer", "LensFlare", "Light", "MonoBehaviour", "Terrain", "NavMeshAgent", "NetworkView",
		"Projector", "Skybox", "Cloth", "InteractiveCloth", "SkinnedCloth", "Collider", "BoxCollider", "CapsuleCollider",
		"CharacterController", "MeshCollider", "SphereCollider", "TerrainCollider", "WheelCollider", "Joint", "CharacterJoint",
		"ConfigurableJoint", "FixedJoint", "HingeJoint", "SpringJoint", "LODGroup", "LightProbeGroup", "MeshFilter",
		"OcclusionArea", "OcclusionPortal", "OffMeshLink", "ParticleAnimator", "ParticleEmitter", "ParticleSystem", "Renderer",
		"ClothRenderer", "LineRenderer", "MeshRenderer", "ParticleRenderer", "ParticleSystemRenderer", "SkinnedMeshRenderer",
		"TrailRenderer", "Rigidbody", "TextMesh", "Transform", "Tree", "Flare", "Font", "GameObject", "LightProbes",
		"Material", "ProceduralMaterial", "Mesh", "NavMesh", "PhysicMaterial", "QualitySettings", "ScriptableObject",
		"GUISkin", "Shader", "TerrainData", "TextAsset", "Texture", "Cubemap", "MovieTexture", "RenderTexture", "Texture2D",
		"WebCamTexture", "OffMeshLinkData", "ParticleSystem", "Particle", "Path", "Physics", "Ping", "Plane",
		"PlayerPrefsException", "PlayerPrefs", "ProceduralPropertyDescription", "Profiler", "Quaternion", "Random", "Range",
		"Ray", "RaycastHit", "RectOffset", "Rect", "RemoteNotification", "RenderBuffer", "RenderSettings", "Resolution",
		"Resources", "Screen", "Security", "SleepTimeout", "Social", "SoftJointLimit", "SplatPrototype",
		"StaticBatchingUtility", "String", "SystemInfo", "Time", "TouchScreenKeyboard", "Touch", "TreeInstance",
		"TreePrototype", "Vector2", "Vector3", "Vector4", "WWWForm", "WWW", "WebCamDevice", "WheelFrictionCurve", "WheelHit",
		"YieldInstruction", "AsyncOperation", "AssetBundleCreateRequest", "AssetBundleRequest", "Coroutine",
		"WaitForEndOfFrame", "WaitForFixedUpdate", "WaitForSeconds", "iPhoneInput", "iPhoneSettings", "iPhoneUtils", "iPhone",

		// Runtime attributes
		"AddComponentMenu", "ContextMenu", "ExecuteInEditMode", "HideInInspector", "ImageEffectOpaque",
		"ImageEffectTransformsToLDR", "InitializeOnLoad", "NonSerialized", "NotConvertedAttribute", "NotRenamedAttribute",
		"RPC", "RequireComponent", "Serializable", "SerializeField",

		// Runtime enumerations
		"ADErrorCode", "ADPosition", "ADSizeIdentifier", "AnimationBlendMode", "AnimationCullingType", "AnisotropicFiltering",
		"AudioReverbPreset", "AudioRolloffMode", "AudioSpeakerMode", "AudioType", "AudioVelocityUpdateMode", "BlendWeights",
		"CalendarIdentifier", "CalendarUnit", "CameraClearFlags", "CollisionDetectionMode", "CollisionFlags", "ColorSpace",
		"ConfigurableJointMotion", "ConnectionTesterStatus", "CubemapFace", "DepthTextureMode", "DetailRenderMode",
		"DeviceOrientation", "DeviceType", "EventModifiers", "EventType", "FFTWindow", "FilterMode", "FocusType", "FogMode",
		"FontStyle", "ForceMode", "FullScreenMovieControlMode", "FullScreenMovieScalingMode", "HideFlags", "IMECompositionMode",
		"ImagePosition", "JointDriveMode", "JointProjectionMode", "KeyCode", "LightRenderMode", "LightShadows", "LightType",
		"LightmapsMode", "LocationServiceStatus", "LogType", "MasterServerEvent", "NavMeshPathStatus", "NetworkConnectionError",
		"NetworkDisconnection", "NetworkLogLevel", "NetworkPeerType", "NetworkReachability", "NetworkStateSynchronization",
		"ObstacleAvoidanceType", "OffMeshLinkType", "ParticleRenderMode", "ParticleSystemRenderMode", "PhysicMaterialCombine",
		"PlayMode", "PrimitiveType", "ProceduralCacheSize", "ProceduralProcessorUsage", "ProceduralPropertyType", "QueueMode",
		"RPCMode", "RemoteNotificationType", "RenderTextureFormat", "RenderTextureReadWrite", "RenderingPath",
		"RigidbodyConstraints", "RigidbodyInterpolation", "RotationDriveMode", "RuntimePlatform", "ScaleMode",
		"ScreenOrientation", "SendMessageOptions", "ShadowProjection", "SkinQuality", "Space", "SystemLanguage", "TextAlignment",
		"TextAnchor", "TextClipping", "TextureCompressionQuality", "TextureFormat", "TextureWrapMode", "ThreadPriority",
		"TimeScope", "TouchPhase", "TouchScreenKeyboardType", "UserAuthorization", "UserScope", "UserState", "WrapMode",
		"iPhoneGeneration",

		// Editor classes
		"AnimationClipCurveData", "AnimationUtility", "ArrayUtility", "AssetDatabase", "AssetImporter", "AudioImporter",
		"ModelImporter", "MovieImporter", "SubstanceImporter", "TextureImporter", "TrueTypeFontImporter",
		"AssetModificationProcessor", "AssetPostprocessor", "AssetStore", "BuildPipeline", "DragAndDrop", "EditorApplication",
		"EditorBuildSettings", "EditorGUILayout", "EditorGUIUtility", "EditorGUI", "EditorPrefs", "EditorStyles",
		"EditorUserBuildSettings", "EditorUtility", "EditorWindow", "ScriptableWizard", "Editor", "FileUtil",
		"GameObjectUtility", "GenericMenu", "HandleUtility", "Handles", "Help", "LODUtility", "LightmapEditorSettings",
		"Lightmapping", "MenuCommand", "MeshUtility", "ModelImporterClipAnimation", "MonoScript", "NavMeshBuilder",
		"ObjectNames", "Android", "Wii", "iOS", "PlayerSettings", "PrefabUtility", "ProceduralTexture", "PropertyModification",
		"Selection", "SerializedObject", "SerializedProperty", "StaticOcclusionCullingVisualization", "StaticOcclusionCulling",
		"SubstanceArchive", "TextureImporterSettings", "Tools", "Undo", "UnwrapParam", "Unwrapping",

		// Editor attributes
		"CanEditMultipleObjects", "CustomEditor", "DrawGizmo", "MenuItem", "PreferenceItem",

		// Editor enumerations
		"AndroidBuildSubtarget", "AndroidPreferredInstallLocation", "AndroidSdkVersions",
		"AndroidShowActivityIndicatorOnLoading", "AndroidSplashScreenScale", "AndroidTargetDevice", "AndroidTargetGraphics",
		"ApiCompatibilityLevel", "AspectRatio", "AssetDeleteResult", "AssetMoveResult", "AudioImporterFormat",
		"AudioImporterLoadType", "BuildAssetBundleOptions", "BuildOptions", "BuildTargetGroup", "BuildTarget",
		"DragAndDropVisualMode", "DrawCameraMode", "EditorSkin", "ExportPackageOptions", "FontRenderMode", "FontTextureCase",
		"GizmoType", "ImportAssetOptions", "InspectorMode", "LightmapBakeQuality", "MessageType",
		"ModelImporterAnimationCompression", "ModelImporterGenerateAnimations", "ModelImporterMaterialName",
		"ModelImporterMaterialSearch", "ModelImporterMeshCompression", "ModelImporterTangentSpaceMode", "MouseCursor",
		"PS3BuildSubtarget", "PivotMode", "PivotRotation", "PrefabType", "ProceduralOutputType", "RemoveAssetOptions",
		"ReplacePrefabOptions", "ResolutionDialogSetting", "ScriptCallOptimizationLevel", "SelectionMode",
		"SerializedPropertyType", "StaticEditorFlags", "StaticOcclusionCullingMode", "StrippingLevel", "TextureImporterFormat",
		"TextureImporterGenerateCubemap", "TextureImporterMipFilter", "TextureImporterNPOTScale", "TextureImporterNormalFilter",
		"TextureImporterType", "Tool", "UIOrientation", "ViewTool", "WiiBuildDebugLevel", "WiiBuildSubtarget", "WiiHio2Usage",
		"WiiMemoryArea", "WiiMemoryLabel", "WiiRegion", "XboxBuildSubtarget", "XboxRunMethod", "iOSSdkVersion",
		"iOSShowActivityIndicatorOnLoading", "iOSStatusBarStyle", "iOSTargetDevice", "iOSTargetOSVersion", "iOSTargetPlatform",
		"iOSTargetResolution", 
	};*/
	#endregion

	protected bool IsPreprocessorDirective(string word)
	{
		return Array.BinarySearch(PreprocessorDirectives, word, textBuffer.isShader ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal) >= 0;
	}

	protected bool IsKeyword(string word)
	{
		return Array.BinarySearch(Keywords, word, textBuffer.isShader ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal) >= 0;
	}

	public bool IsBuiltInLiteral(string word)
	{
		return Array.BinarySearch(BuiltInLiterals, word, textBuffer.isShader ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal) >= 0;
	}

	public bool IsBuiltInType(string word)
	{
		return Array.BinarySearch(BuiltInTypes, word, textBuffer.isShader ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal) >= 0;
	}

	protected bool IsUnityType(string word)
	{
		return textBuffer.isShader ? false : Array.BinarySearch(unityTypes, word) >= 0;
	}

	public virtual string GetToken(int n)
	{
		return "!!!";
	}

	public virtual int TokenToId(string tokenText)
	{
		return 0;
	}

	public Func<bool> Update(int fromLine, int toLineInclusive)
	{
	//	var t = new Stopwatch();
	//	t.Start();

//		// TODO: Optimize this
//		for (var i = fromLine; i < textBuffer.lines.Count; ++i)
//		{
//			var line = textBuffer.formatedLines[i];
//			if (line == null)
//				continue;
//
//			for (var j = 0; j < line.tokens.Count; ++j)
//			{
//				var token = line.tokens[j];
//				if (token.parent != null)
//				{
//					token.parent.line = i;
//					token.parent.tokenIndex = j;
//				}
//			}
//		}

		//var line = fromLine;
		//while (line <= toLineInclusive)
		//    if (!ParseLine(line++))
		//        break;

		//if (line >= toLineInclusive)
		//    while (line < textBuffer.formatedLines.Length)
		//        if (!ParseLine(line++))
		//            break;

		var lastLine = textBuffer.formatedLines.Length - 1;
		try
		{
			if (this.parseTree != null)
				ParseLines(fromLine, lastLine);
		}
		catch (Exception e)
		{
			Debug.LogException(e);
		}
		//if (toLineInclusive < textBuffer.lines.Count)
		//{
		//    progressiveParseLine = toLineInclusive + 1;
		//    return ProgressiveParser;
		//}
		//else
		{
			// TODO: Temporary solution, discarding all unused invalid parse tree nodes
			if (parseTree != null && parseTree.root != null)
				parseTree.root.CleanUp();
		}
		//BuildTopLevelSyntaxTree(assetPath);
		//RunSemanticAnalysis(assetPath);

	//	t.Stop();
	//	Debug.Log("Updated parser for lines " + (fromLine + 1) + "-" + (toLineInclusive + 1) + " in " + t.ElapsedMilliseconds + " ms");
		return null;
	}

	int progressiveParseLine = -1;
	bool ProgressiveParser()
	{
		if (textBuffer == null || textBuffer.lines == null || textBuffer.lines.Count <= progressiveParseLine)
		{
			progressiveParseLine = -1;
			return false;
		}

		if (!ParseLines(progressiveParseLine, progressiveParseLine))
			return false;
		++progressiveParseLine;
		if (progressiveParseLine < textBuffer.lines.Count)
			return true;

		progressiveParseLine = -1;
		return false;
	}

	protected static SyntaxToken ScanWhitespace(string line, ref int startAt)
	{
		if (startAt >= line.Length)
			return SyntaxToken.CreateMissing();

		int i = startAt;
		while (i < line.Length && (line[i] == ' ' || line[i] == '\t'))
			++i;
		var token = new SyntaxToken(SyntaxToken.Kind.Whitespace, line.Substring(startAt, i - startAt));
		startAt = i;
		return token;
	}

	protected static SyntaxToken ScanWord(string line, ref int startAt)
	{
		int i = startAt;
		while (i < line.Length)
		{
			if (!Char.IsLetterOrDigit(line, i) && line[i] != '_')
				break;
			++i;
		}
		var token = new SyntaxToken(SyntaxToken.Kind.Identifier, line.Substring(startAt, i - startAt));
		startAt = i;
		return token;
	}

	protected static bool ScanUnicodeEscapeChar(string line, ref int startAt)
	{
		if (startAt >= line.Length - 5)
			return false;
		if (line[startAt] != '\\')
			return false;
		int i = startAt + 1;
		/*if (line[i] == 'x' || line[i] == 'X')
		{
			++i;
			for (var n = 0; n < 8; ++n)
				if (!ScanHexDigit(line, ref i))
					break;
			if (i <= startAt + 2)
				return false;
			startAt = i;
			return true;
		}
		else*/
		if (line[i] != 'u' && line[i] != 'U')
			return false;
		var n = line[i] == 'u' ? 4 : 8;
		++i;
		while (n > 0)
		{
			if (!ScanHexDigit(line, ref i))
				break;
			--n;
		}
		if (n == 0)
		{
			startAt = i;
			return true;
		}
		return false;
	}

	protected static SyntaxToken ScanCharLiteral(string line, ref int startAt)
	{
		//if (startAt >= line.Length - 1 || line[startAt] != '\'')
		//	return SyntaxToken.CreateMissing();
		var i = startAt + 1;
		while (i < line.Length)
		{
			if (line[i] == '\'')
			{
				++i;
				break;
			}
			if (line[i] == '\\' && i < line.Length - 1)
				++i;
			++i;
		}
		var token = new SyntaxToken(SyntaxToken.Kind.CharLiteral, line.Substring(startAt, i - startAt));
		startAt = i;
		return token;
	}

	protected static SyntaxToken ScanStringLiteral(string line, ref int startAt)
	{
		//if (startAt >= line.Length - 1 || line[startAt] != '\"')
		//	return SyntaxToken.CreateMissing();
		var i = startAt + 1;
		while (i < line.Length)
		{
			if (line[i] == '\"')
			{
				++i;
				break;
			}
			if (line[i] == '\\' && i < line.Length - 1)
				++i;
			++i;
		}
		var token = new SyntaxToken(SyntaxToken.Kind.StringLiteral, line.Substring(startAt, i - startAt));
		startAt = i;
		return token;
	}

	protected static SyntaxToken ScanNumericLiteral(string line, ref int startAt)
	{
		bool hex = false;
		bool point = false;
		bool exponent = false;
		var i = startAt;

		SyntaxToken token;

		char c;
		if (line[i] == '0' && i < line.Length - 1 && (line[i + 1] == 'x' || line[i + 1] == 'X'))
		{
			i += 2;
			hex = true;
			while (i < line.Length)
			{
				c = line[i];
				if (c >= '0' && c <= '9' || c >= 'a' && c <= 'f' || c >= 'A' && c <= 'F')
					++i;
				else
					break;
			}
		}
		else
		{
			while (i < line.Length && line[i] >= '0' && line[i] <= '9')
				++i;
		}

		if (i > startAt && i < line.Length)
		{
			c = line[i];
			if (c == 'l' || c == 'L' || c == 'u' || c == 'U')
			{
				++i;
				if (i < line.Length)
				{
					if (c == 'l' || c == 'L')
					{
						if (line[i] == 'u' || line[i] == 'U')
							++i;
					}
					else if (line[i] == 'l' || line[i] == 'L')
						++i;
				}
				token = new SyntaxToken(SyntaxToken.Kind.IntegerLiteral, line.Substring(startAt, i - startAt));
				startAt = i;
				return token;
			}
		}

		if (hex)
		{
			token = new SyntaxToken(SyntaxToken.Kind.IntegerLiteral, line.Substring(startAt, i - startAt));
			startAt = i;
			return token;
		}

		while (i < line.Length)
		{
			c = line[i];

			if (!point && !exponent && c == '.')
			{
				if (i < line.Length - 1 && line[i+1] >= '0' && line[i+1] <= '9')
				{
					point = true;
					++i;
					continue;
				}
				else
				{
					break;
				}
			}
			if (!exponent && i > startAt && (c == 'e' || c == 'E'))
			{
				exponent = true;
				++i;
				if (i < line.Length && (line[i] == '-' || line[i] == '+'))
					++i;
				continue;
			}
			if (c == 'f' || c == 'F' || c == 'd' || c == 'D' || c == 'm' || c == 'M')
			{
				point = true;
				++i;
				break;
			}
			if (c < '0' || c > '9')
				break;
			++i;
		}
		token = new SyntaxToken(point || exponent ? SyntaxToken.Kind.RealLiteral : SyntaxToken.Kind.IntegerLiteral,
		                        line.Substring(startAt, i - startAt));
		startAt = i;
		return token;
	}

	protected static bool ScanHexDigit(string line, ref int i)
	{
		if (i >= line.Length)
			return false;
		char c = line[i];
		if (c >= '0' && c <= '9' || c >= 'A' && c <= 'F' || c >= 'a' && c <= 'f')
		{
			++i;
			return true;
		}
		return false;
	}

	protected static SyntaxToken ScanIdentifierOrKeyword(string line, ref int startAt)
	{
		bool identifier = false;
		int i = startAt;
		if (i < line.Length)
		{
			char c = line[i];
			if (c == '@')
			{
				identifier = true;
				++i;
			}
			if (i < line.Length)
			{
				c = line[i];
				if (char.IsLetter(c) || c == '_')
					++i;
				else if (!ScanUnicodeEscapeChar(line, ref i))
					return SyntaxToken.CreateMissing();
				else
					identifier = true;

				while (i < line.Length)
				{
					if (char.IsLetterOrDigit(line, i) || line[i] == '_')
						++i;
					else if (!ScanUnicodeEscapeChar(line, ref i))
						break;
					else
						identifier = true;
				}
			}
		}
		var word = line.Substring(startAt, i - startAt);
		startAt = i;
		return new SyntaxToken(identifier ? SyntaxToken.Kind.Identifier : SyntaxToken.Kind.Keyword, word);
	}
}

internal class JsParser : FGParser
{
	public override void CutParseTree(int fromLine, FGTextBuffer.FormatedLine[] formatedLines) {}
}

internal class BooParser : FGParser
{
	public override void CutParseTree(int fromLine, FGTextBuffer.FormatedLine[] formatedLines) {}
}

internal class ShaderParser : FGParser
{
	public override void CutParseTree(int fromLine, FGTextBuffer.FormatedLine[] formatedLines) {}
}

internal class TextParser : FGParser
{
	public override void CutParseTree(int fromLine, FGTextBuffer.FormatedLine[] formatedLines) {}
}

}
