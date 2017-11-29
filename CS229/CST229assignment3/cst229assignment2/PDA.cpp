#include "stdafx.h"
#include "PDA.h"
#include <string>

using namespace std;

DFA::DFA(char is)
{
	this->initialState = is;
}
DFA::~DFA()
{

}
DFA::Transition::Transition(char c, char i, char n) :currentstate(c), inputchar(i), nextstate(n)
{

}
void DFA::add_transistion(char currentState, char inputchar, char destinationstate)
{
	//transitions.insert(Transition(currentState, inputchar, destinationstate), MakeKey(currentState, inputchar));
	std::string key = MakeKey(currentState, inputchar);
	transitions[key] = destinationstate;
}
void DFA::SetFinalState(char finalstate)
{
	finalStates.insert(finalstate);
}
string DFA::MakeKey(char currentState, char inputchar)
{
	std::string key;
	key += currentState;
	key += inputchar;
	return key;
}
bool DFA::Accept(string s)
{
	char currentState = initialState;
	for each (char inputChar in s)
	{
		string key = MakeKey(currentState, inputChar);

		if (transitions.find(key) != transitions.end())
		{
			currentState = transitions[key];
		}
		else
		{
			return false;
		}

		

	}
	return finalStates.find(currentState) != finalStates.end();
}
