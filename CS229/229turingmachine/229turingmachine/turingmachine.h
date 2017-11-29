#pragma once
#include "stdafx.h"
#include <stack>
#include <map>
#include <vector>
using namespace std;
class turingmachine
{
public:
	turingmachine(char startingstate);
	~turingmachine();
	void SetFinalState(char finalstate);
	bool Accept(string s);
	void AddTransition(char currentstate, char nextstate, char inputchar, char outputchar, char Dir);
private:
	class transition
	{
		char currentstate;//Part of key
		char nextstate;//Value of Key[0]
		char inputchar;//Part of key
		char outputchar;//Value of Key[1]
		char Dir;//Value of Key[2]
		transition(char s, char e, char i, char o, char d){ currentstate = s; nextstate = e; inputchar = i; outputchar = o; Dir = d;}
	};
	char StartingState;
	char FinalState;
	string MakeKey(char startingstate, char inputchar);
	string MakeKeyValue(char nextstate, char outputchar, char dir);
	map<string, string> transitions; //transitions with keybeing startingstate/inputchar
	
};