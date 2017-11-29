// cst229assignment2.cpp 
//

#include "stdafx.h"
#include "DFA.h"
#include <string>
#include <list>
#include <iostream>
using namespace std;
int main()
{
	list<string> transitions;
	list<char> finalStates;
	list<string> inputStrings;
	string line;
	int nStates = 0;
	std::getline(cin, line);
	std::getline(std::cin, line);
	nStates = atoi(line.c_str());
	std::getline(std::cin, line);
	while (line[0] != 'F')
	{
		transitions.push_back(line);
		std::getline(std::cin, line);
	}
	for (unsigned int i = 3; i < line.length(); i = i + 3)
	{
		finalStates.push_back(line[i]);
	}

	while (getline(cin, line))  
	{

		inputStrings.push_back(line);
	}
	//read input file

	DFA dfa(transitions.front()[0]);
	for each(string t in transitions)
	{
		dfa.add_transistion(t[0], t[3], t[6]);

	}
	for each(char f in finalStates)
	{
		dfa.SetFinalState(f);
	}
	//create DFA
	for each (std::string s in inputStrings)
	{
		if (dfa.Accept(s))
		{
			cout << "Accept: \t" << s << endl;
		}
		else
		{
			cout << "Not Accepted:<<" << s<<endl; 
		}
	}
	//process each string and print whether accept or reject

    return 0;
}

