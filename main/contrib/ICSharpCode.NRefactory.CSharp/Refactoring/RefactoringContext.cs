﻿// 
// RefactoringContext.cs
//
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Mike Krüger <mkrueger@novell.com>
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Linq;
using System.Threading;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using ICSharpCode.NRefactory.Editor;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	public abstract class RefactoringContext
	{
		readonly CSharpAstResolver resolver;
		readonly CancellationToken cancellationToken;
		
		public RefactoringContext(CSharpAstResolver resolver, CancellationToken cancellationToken)
		{
			this.resolver = resolver;
			this.cancellationToken = cancellationToken;
		}
		
		public CancellationToken CancellationToken {
			get { return cancellationToken; }
		}
		
		public virtual AstNode RootNode {
			get { return resolver.RootNode; }
		}

		public abstract TextLocation Location { get; }
		
		public virtual bool Supports(Version version)
		{
			return true;
		}
		
		public ICompilation Compilation {
			get { return resolver.Compilation; }
		}
		
		public virtual AstType CreateShortType (IType fullType)
		{
			var csResolver = resolver.GetResolverStateBefore(GetNode());
			var builder = new TypeSystemAstBuilder(csResolver);
			return builder.ConvertType(fullType);
		}
		
		public AstType CreateShortType (string ns, string name, int typeParameterCount = 0)
		{
			foreach (var asm in Compilation.Assemblies) {
				var def = asm.GetTypeDefinition (ns, name, typeParameterCount);
				if (def != null)
					return CreateShortType (def);
			}
			
			return new MemberType (new SimpleType (ns), name);
		}
		
		public AstNode GetNode ()
		{
			return RootNode.GetNodeAt (Location);
		}
		
		public T GetNode<T> () where T : AstNode
		{
			return RootNode.GetNodeAt<T> (Location);
		}
		
		#region Text stuff
		public virtual string EolMarker {
			get { return Environment.NewLine; }
		}
		
		public virtual bool IsSomethingSelected {
			get { return this.SelectionLength > 0; }
		}
		
		public virtual string SelectedText {
			get { return string.Empty; }
		}
		
		public virtual int SelectionStart {
			get { return 0; }
		}
		public virtual int SelectionEnd {
			get { return 0; }
		}
		public virtual int SelectionLength {
			get { return 0; }
		}

		public abstract int GetOffset (TextLocation location);

		public abstract IDocumentLine GetLineByOffset (int offset);
		
		public int GetOffset (int line, int col)
		{
			return GetOffset (new TextLocation (line, col));
		}

		public abstract TextLocation GetLocation (int offset);

		public abstract string GetText (int offset, int length);

		public abstract string GetText (ISegment segment);
		#endregion
		
		#region Resolving
		public ResolveResult Resolve (AstNode node)
		{
			return resolver.Resolve (node, cancellationToken);
		}
		
		public IType ResolveType (AstType type)
		{
			return resolver.Resolve (type, cancellationToken).Type;
		}
		
		public IType GetExpectedType (Expression expression)
		{
			return resolver.GetExpectedType(expression, cancellationToken);
		}
		
		public Conversion GetConversion (Expression expression)
		{
			return resolver.GetConversion(expression, cancellationToken);
		}
		#endregion
		
		public virtual string GetNameProposal (string name, bool camelCase = true)
		{
			string baseName = (camelCase ? char.ToLower (name [0]) : char.ToUpper (name [0])) + name.Substring (1);
			
			var type = GetNode<TypeDeclaration> ();
			if (type == null)
				return baseName;
			
			int number = -1;
			string proposedName;
			do {
				proposedName = AppendNumberToName (baseName, number++);
			} while (type.Members.Select (m => m.GetChildByRole (Roles.Identifier)).Any (n => n.Name == proposedName));
			return proposedName;
		}
		
		static string AppendNumberToName (string baseName, int number)
		{
			return baseName + (number > 0 ? (number + 1).ToString () : "");
		}
		
		public abstract Script StartScript();
	}
}
