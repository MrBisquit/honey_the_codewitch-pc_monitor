﻿Object = "{"
ObjectEnd = "}"
Array = "["
ArrayEnd = "]"
FieldSeparator = ":"
Comma = ","
Number = '-?(?:0|[1-9][0-9]*)(?:\.[0-9]+)?(?:[eE][+-]?[0-9]+)?'
Boolean = 'true|false'
Null = "null"
String = '"([^\n"\\]|\\([btrnf"\\/]|(u[0-9A-Fa-f]{4})))*"'
WhiteSpace = '[ \t\r\n]+'
CommentLine = '\/\/[^\n]*'
CommentBlock<blockEnd="*/"> = "/*"