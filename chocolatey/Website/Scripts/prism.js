/* PrismJS 1.15.0
https://prismjs.com/download.html#themes=prism-coy&languages=markup+css+clike+javascript+powershell&plugins=line-numbers+toolbar+unescaped-markup+copy-to-clipboard */
var _self = (typeof window !== 'undefined')
    ? window   // if in browser
    : (
        (typeof WorkerGlobalScope !== 'undefined' && self instanceof WorkerGlobalScope)
            ? self // if in worker
            : {}   // if in node js
    );

/**
 * Prism: Lightweight, robust, elegant syntax highlighting
 * MIT license http://www.opensource.org/licenses/mit-license.php/
 * @author Lea Verou http://lea.verou.me
 */

var Prism = (function () {

    // Private helper vars
    var lang = /\blang(?:uage)?-([\w-]+)\b/i;
    var uniqueId = 0;

    var _ = _self.Prism = {
        manual: _self.Prism && _self.Prism.manual,
        disableWorkerMessageHandler: _self.Prism && _self.Prism.disableWorkerMessageHandler,
        util: {
            encode: function (tokens) {
                if (tokens instanceof Token) {
                    return new Token(tokens.type, _.util.encode(tokens.content), tokens.alias);
                } else if (_.util.type(tokens) === 'Array') {
                    return tokens.map(_.util.encode);
                } else {
                    return tokens.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/\u00a0/g, ' ');
                }
            },

            type: function (o) {
                return Object.prototype.toString.call(o).slice(8, -1);
            },

            objId: function (obj) {
                if (!obj['__id']) {
                    Object.defineProperty(obj, '__id', { value: ++uniqueId });
                }
                return obj['__id'];
            },

            // Deep clone a language definition (e.g. to extend it)
            clone: function (o, visited) {
                var type = _.util.type(o);
                visited = visited || {};

                switch (type) {
                    case 'Object':
                        if (visited[_.util.objId(o)]) {
                            return visited[_.util.objId(o)];
                        }
                        var clone = {};
                        visited[_.util.objId(o)] = clone;

                        for (var key in o) {
                            if (o.hasOwnProperty(key)) {
                                clone[key] = _.util.clone(o[key], visited);
                            }
                        }

                        return clone;

                    case 'Array':
                        if (visited[_.util.objId(o)]) {
                            return visited[_.util.objId(o)];
                        }
                        var clone = [];
                        visited[_.util.objId(o)] = clone;

                        o.forEach(function (v, i) {
                            clone[i] = _.util.clone(v, visited);
                        });

                        return clone;
                }

                return o;
            }
        },

        languages: {
            extend: function (id, redef) {
                var lang = _.util.clone(_.languages[id]);

                for (var key in redef) {
                    lang[key] = redef[key];
                }

                return lang;
            },

            /**
             * Insert a token before another token in a language literal
             * As this needs to recreate the object (we cannot actually insert before keys in object literals),
             * we cannot just provide an object, we need an object and a key.
             * @param inside The key (or language id) of the parent
             * @param before The key to insert before.
             * @param insert Object with the key/value pairs to insert
             * @param root The object that contains `inside`. If equal to Prism.languages, it can be omitted.
             */
            insertBefore: function (inside, before, insert, root) {
                root = root || _.languages;
                var grammar = root[inside];
                var ret = {};

                for (var token in grammar) {
                    if (grammar.hasOwnProperty(token)) {

                        if (token == before) {
                            for (var newToken in insert) {
                                if (insert.hasOwnProperty(newToken)) {
                                    ret[newToken] = insert[newToken];
                                }
                            }
                        }

                        // Do not insert token which also occur in insert. See #1525
                        if (!insert.hasOwnProperty(token)) {
                            ret[token] = grammar[token];
                        }
                    }
                }

                var old = root[inside];
                root[inside] = ret;

                // Update references in other language definitions
                _.languages.DFS(_.languages, function (key, value) {
                    if (value === old && key != inside) {
                        this[key] = ret;
                    }
                });

                return ret;
            },

            // Traverse a language definition with Depth First Search
            DFS: function (o, callback, type, visited) {
                visited = visited || {};
                for (var i in o) {
                    if (o.hasOwnProperty(i)) {
                        callback.call(o, i, o[i], type || i);

                        if (_.util.type(o[i]) === 'Object' && !visited[_.util.objId(o[i])]) {
                            visited[_.util.objId(o[i])] = true;
                            _.languages.DFS(o[i], callback, null, visited);
                        }
                        else if (_.util.type(o[i]) === 'Array' && !visited[_.util.objId(o[i])]) {
                            visited[_.util.objId(o[i])] = true;
                            _.languages.DFS(o[i], callback, i, visited);
                        }
                    }
                }
            }
        },
        plugins: {},

        highlightAll: function (async, callback) {
            _.highlightAllUnder(document, async, callback);
        },

        highlightAllUnder: function (container, async, callback) {
            var env = {
                callback: callback,
                selector: 'code[class*="language-"], [class*="language-"] code, code[class*="lang-"], [class*="lang-"] code'
            };

            _.hooks.run("before-highlightall", env);

            var elements = env.elements || container.querySelectorAll(env.selector);

            for (var i = 0, element; element = elements[i++];) {
                _.highlightElement(element, async === true, env.callback);
            }
        },

        highlightElement: function (element, async, callback) {
            // Find language
            var language, grammar, parent = element;

            while (parent && !lang.test(parent.className)) {
                parent = parent.parentNode;
            }

            if (parent) {
                language = (parent.className.match(lang) || [, ''])[1].toLowerCase();
                grammar = _.languages[language];
            }

            // Set language on the element, if not present
            element.className = element.className.replace(lang, '').replace(/\s+/g, ' ') + ' language-' + language;

            if (element.parentNode) {
                // Set language on the parent, for styling
                parent = element.parentNode;

                if (/pre/i.test(parent.nodeName)) {
                    parent.className = parent.className.replace(lang, '').replace(/\s+/g, ' ') + ' language-' + language;
                }
            }

            var code = element.textContent;

            var env = {
                element: element,
                language: language,
                grammar: grammar,
                code: code
            };

            var insertHighlightedCode = function (highlightedCode) {
                env.highlightedCode = highlightedCode;

                _.hooks.run('before-insert', env);

                env.element.innerHTML = env.highlightedCode;

                _.hooks.run('after-highlight', env);
                _.hooks.run('complete', env);
                callback && callback.call(env.element);
            }

            _.hooks.run('before-sanity-check', env);

            if (!env.code) {
                _.hooks.run('complete', env);
                return;
            }

            _.hooks.run('before-highlight', env);

            if (!env.grammar) {
                insertHighlightedCode(_.util.encode(env.code));
                return;
            }

            if (async && _self.Worker) {
                var worker = new Worker(_.filename);

                worker.onmessage = function (evt) {
                    insertHighlightedCode(evt.data);
                };

                worker.postMessage(JSON.stringify({
                    language: env.language,
                    code: env.code,
                    immediateClose: true
                }));
            }
            else {
                insertHighlightedCode(_.highlight(env.code, env.grammar, env.language));
            }
        },

        highlight: function (text, grammar, language) {
            var env = {
                code: text,
                grammar: grammar,
                language: language
            };
            _.hooks.run('before-tokenize', env);
            env.tokens = _.tokenize(env.code, env.grammar);
            _.hooks.run('after-tokenize', env);
            return Token.stringify(_.util.encode(env.tokens), env.language);
        },

        matchGrammar: function (text, strarr, grammar, index, startPos, oneshot, target) {
            var Token = _.Token;

            for (var token in grammar) {
                if (!grammar.hasOwnProperty(token) || !grammar[token]) {
                    continue;
                }

                if (token == target) {
                    return;
                }

                var patterns = grammar[token];
                patterns = (_.util.type(patterns) === "Array") ? patterns : [patterns];

                for (var j = 0; j < patterns.length; ++j) {
                    var pattern = patterns[j],
                        inside = pattern.inside,
                        lookbehind = !!pattern.lookbehind,
                        greedy = !!pattern.greedy,
                        lookbehindLength = 0,
                        alias = pattern.alias;

                    if (greedy && !pattern.pattern.global) {
                        // Without the global flag, lastIndex won't work
                        var flags = pattern.pattern.toString().match(/[imuy]*$/)[0];
                        pattern.pattern = RegExp(pattern.pattern.source, flags + "g");
                    }

                    pattern = pattern.pattern || pattern;

                    // Don’t cache length as it changes during the loop
                    for (var i = index, pos = startPos; i < strarr.length; pos += strarr[i].length, ++i) {

                        var str = strarr[i];

                        if (strarr.length > text.length) {
                            // Something went terribly wrong, ABORT, ABORT!
                            return;
                        }

                        if (str instanceof Token) {
                            continue;
                        }

                        if (greedy && i != strarr.length - 1) {
                            pattern.lastIndex = pos;
                            var match = pattern.exec(text);
                            if (!match) {
                                break;
                            }

                            var from = match.index + (lookbehind ? match[1].length : 0),
                                to = match.index + match[0].length,
                                k = i,
                                p = pos;

                            for (var len = strarr.length; k < len && (p < to || (!strarr[k].type && !strarr[k - 1].greedy)); ++k) {
                                p += strarr[k].length;
                                // Move the index i to the element in strarr that is closest to from
                                if (from >= p) {
                                    ++i;
                                    pos = p;
                                }
                            }

                            // If strarr[i] is a Token, then the match starts inside another Token, which is invalid
                            if (strarr[i] instanceof Token) {
                                continue;
                            }

                            // Number of tokens to delete and replace with the new match
                            delNum = k - i;
                            str = text.slice(pos, p);
                            match.index -= pos;
                        } else {
                            pattern.lastIndex = 0;

                            var match = pattern.exec(str),
                                delNum = 1;
                        }

                        if (!match) {
                            if (oneshot) {
                                break;
                            }

                            continue;
                        }

                        if (lookbehind) {
                            lookbehindLength = match[1] ? match[1].length : 0;
                        }

                        var from = match.index + lookbehindLength,
                            match = match[0].slice(lookbehindLength),
                            to = from + match.length,
                            before = str.slice(0, from),
                            after = str.slice(to);

                        var args = [i, delNum];

                        if (before) {
                            ++i;
                            pos += before.length;
                            args.push(before);
                        }

                        var wrapped = new Token(token, inside ? _.tokenize(match, inside) : match, alias, match, greedy);

                        args.push(wrapped);

                        if (after) {
                            args.push(after);
                        }

                        Array.prototype.splice.apply(strarr, args);

                        if (delNum != 1)
                            _.matchGrammar(text, strarr, grammar, i, pos, true, token);

                        if (oneshot)
                            break;
                    }
                }
            }
        },

        tokenize: function (text, grammar) {
            var strarr = [text];

            var rest = grammar.rest;

            if (rest) {
                for (var token in rest) {
                    grammar[token] = rest[token];
                }

                delete grammar.rest;
            }

            _.matchGrammar(text, strarr, grammar, 0, 0, false);

            return strarr;
        },

        hooks: {
            all: {},

            add: function (name, callback) {
                var hooks = _.hooks.all;

                hooks[name] = hooks[name] || [];

                hooks[name].push(callback);
            },

            run: function (name, env) {
                var callbacks = _.hooks.all[name];

                if (!callbacks || !callbacks.length) {
                    return;
                }

                for (var i = 0, callback; callback = callbacks[i++];) {
                    callback(env);
                }
            }
        }
    };

    var Token = _.Token = function (type, content, alias, matchedStr, greedy) {
        this.type = type;
        this.content = content;
        this.alias = alias;
        // Copy of the full string this token was created from
        this.length = (matchedStr || "").length | 0;
        this.greedy = !!greedy;
    };

    Token.stringify = function (o, language, parent) {
        if (typeof o == 'string') {
            return o;
        }

        if (_.util.type(o) === 'Array') {
            return o.map(function (element) {
                return Token.stringify(element, language, o);
            }).join('');
        }

        var env = {
            type: o.type,
            content: Token.stringify(o.content, language, parent),
            tag: 'span',
            classes: ['token', o.type],
            attributes: {},
            language: language,
            parent: parent
        };

        if (o.alias) {
            var aliases = _.util.type(o.alias) === 'Array' ? o.alias : [o.alias];
            Array.prototype.push.apply(env.classes, aliases);
        }

        _.hooks.run('wrap', env);

        var attributes = Object.keys(env.attributes).map(function (name) {
            return name + '="' + (env.attributes[name] || '').replace(/"/g, '&quot;') + '"';
        }).join(' ');

        return '<' + env.tag + ' class="' + env.classes.join(' ') + '"' + (attributes ? ' ' + attributes : '') + '>' + env.content + '</' + env.tag + '>';

    };

    if (!_self.document) {
        if (!_self.addEventListener) {
            // in Node.js
            return _self.Prism;
        }

        if (!_.disableWorkerMessageHandler) {
            // In worker
            _self.addEventListener('message', function (evt) {
                var message = JSON.parse(evt.data),
                    lang = message.language,
                    code = message.code,
                    immediateClose = message.immediateClose;

                _self.postMessage(_.highlight(code, _.languages[lang], lang));
                if (immediateClose) {
                    _self.close();
                }
            }, false);
        }

        return _self.Prism;
    }

    //Get current script and highlight
    var script = document.currentScript || [].slice.call(document.getElementsByTagName("script")).pop();

    if (script) {
        _.filename = script.src;

        if (!_.manual && !script.hasAttribute('data-manual')) {
            if (document.readyState !== "loading") {
                if (window.requestAnimationFrame) {
                    window.requestAnimationFrame(_.highlightAll);
                } else {
                    window.setTimeout(_.highlightAll, 16);
                }
            }
            else {
                document.addEventListener('DOMContentLoaded', _.highlightAll);
            }
        }
    }

    return _self.Prism;

})();

if (typeof module !== 'undefined' && module.exports) {
    module.exports = Prism;
}

// hack for components to work correctly in node.js
if (typeof global !== 'undefined') {
    global.Prism = Prism;
}
;
Prism.languages.markup = {
    'comment': /<!--[\s\S]*?-->/,
    'prolog': /<\?[\s\S]+?\?>/,
    'doctype': /<!DOCTYPE[\s\S]+?>/i,
    'cdata': /<!\[CDATA\[[\s\S]*?]]>/i,
    'tag': {
        pattern: /<\/?(?!\d)[^\s>\/=$<%]+(?:\s+[^\s>\/=]+(?:=(?:("|')(?:\\[\s\S]|(?!\1)[^\\])*\1|[^\s'">=]+))?)*\s*\/?>/i,
        greedy: true,
        inside: {
            'tag': {
                pattern: /^<\/?[^\s>\/]+/i,
                inside: {
                    'punctuation': /^<\/?/,
                    'namespace': /^[^\s>\/:]+:/
                }
            },
            'attr-value': {
                pattern: /=(?:("|')(?:\\[\s\S]|(?!\1)[^\\])*\1|[^\s'">=]+)/i,
                inside: {
                    'punctuation': [
                        /^=/,
                        {
                            pattern: /(^|[^\\])["']/,
                            lookbehind: true
                        }
                    ]
                }
            },
            'punctuation': /\/?>/,
            'attr-name': {
                pattern: /[^\s>\/]+/,
                inside: {
                    'namespace': /^[^\s>\/:]+:/
                }
            }

        }
    },
    'entity': /&#?[\da-z]{1,8};/i
};

Prism.languages.markup['tag'].inside['attr-value'].inside['entity'] =
    Prism.languages.markup['entity'];

// Plugin to make entity title show the real entity, idea by Roman Komarov
Prism.hooks.add('wrap', function (env) {

    if (env.type === 'entity') {
        env.attributes['title'] = env.content.replace(/&amp;/, '&');
    }
});

Prism.languages.xml = Prism.languages.extend('markup', {});
Prism.languages.html = Prism.languages.markup;
Prism.languages.mathml = Prism.languages.markup;
Prism.languages.svg = Prism.languages.markup;

Prism.languages.css = {
    'comment': /\/\*[\s\S]*?\*\//,
    'atrule': {
        pattern: /@[\w-]+?[\s\S]*?(?:;|(?=\s*\{))/i,
        inside: {
            'rule': /@[\w-]+/
            // See rest below
        }
    },
    'url': /url\((?:(["'])(?:\\(?:\r\n|[\s\S])|(?!\1)[^\\\r\n])*\1|.*?)\)/i,
    'selector': /[^{}\s][^{};]*?(?=\s*\{)/,
    'string': {
        pattern: /("|')(?:\\(?:\r\n|[\s\S])|(?!\1)[^\\\r\n])*\1/,
        greedy: true
    },
    'property': /[-_a-z\xA0-\uFFFF][-\w\xA0-\uFFFF]*(?=\s*:)/i,
    'important': /!important\b/i,
    'function': /[-a-z0-9]+(?=\()/i,
    'punctuation': /[(){};:,]/
};

Prism.languages.css['atrule'].inside.rest = Prism.languages.css;

if (Prism.languages.markup) {
    Prism.languages.insertBefore('markup', 'tag', {
        'style': {
            pattern: /(<style[\s\S]*?>)[\s\S]*?(?=<\/style>)/i,
            lookbehind: true,
            inside: Prism.languages.css,
            alias: 'language-css',
            greedy: true
        }
    });

    Prism.languages.insertBefore('inside', 'attr-value', {
        'style-attr': {
            pattern: /\s*style=("|')(?:\\[\s\S]|(?!\1)[^\\])*\1/i,
            inside: {
                'attr-name': {
                    pattern: /^\s*style/i,
                    inside: Prism.languages.markup.tag.inside
                },
                'punctuation': /^\s*=\s*['"]|['"]\s*$/,
                'attr-value': {
                    pattern: /.+/i,
                    inside: Prism.languages.css
                }
            },
            alias: 'language-css'
        }
    }, Prism.languages.markup.tag);
}
;
Prism.languages.clike = {
    'comment': [
        {
            pattern: /(^|[^\\])\/\*[\s\S]*?(?:\*\/|$)/,
            lookbehind: true
        },
        {
            pattern: /(^|[^\\:])\/\/.*/,
            lookbehind: true,
            greedy: true
        }
    ],
    'string': {
        pattern: /(["'])(?:\\(?:\r\n|[\s\S])|(?!\1)[^\\\r\n])*\1/,
        greedy: true
    },
    'class-name': {
        pattern: /((?:\b(?:class|interface|extends|implements|trait|instanceof|new)\s+)|(?:catch\s+\())[\w.\\]+/i,
        lookbehind: true,
        inside: {
            punctuation: /[.\\]/
        }
    },
    'keyword': /\b(?:if|else|while|do|for|return|in|instanceof|function|new|try|throw|catch|finally|null|break|continue)\b/,
    'boolean': /\b(?:true|false)\b/,
    'function': /\w+(?=\()/,
    'number': /\b0x[\da-f]+\b|(?:\b\d+\.?\d*|\B\.\d+)(?:e[+-]?\d+)?/i,
    'operator': /--?|\+\+?|!=?=?|<=?|>=?|==?=?|&&?|\|\|?|\?|\*|\/|~|\^|%/,
    'punctuation': /[{}[\];(),.:]/
};

Prism.languages.javascript = Prism.languages.extend('clike', {
    'class-name': [
        Prism.languages.clike['class-name'],
        {
            pattern: /(^|[^$\w\xA0-\uFFFF])[_$A-Z\xA0-\uFFFF][$\w\xA0-\uFFFF]*(?=\.(?:prototype|constructor))/,
            lookbehind: true
        }
    ],
    'keyword': [
        {
            pattern: /((?:^|})\s*)(?:catch|finally)\b/,
            lookbehind: true
        },
        /\b(?:as|async|await|break|case|class|const|continue|debugger|default|delete|do|else|enum|export|extends|for|from|function|get|if|implements|import|in|instanceof|interface|let|new|null|of|package|private|protected|public|return|set|static|super|switch|this|throw|try|typeof|var|void|while|with|yield)\b/
    ],
    'number': /\b(?:(?:0[xX][\dA-Fa-f]+|0[bB][01]+|0[oO][0-7]+)n?|\d+n|NaN|Infinity)\b|(?:\b\d+\.?\d*|\B\.\d+)(?:[Ee][+-]?\d+)?/,
    // Allow for all non-ASCII characters (See http://stackoverflow.com/a/2008444)
    'function': /[_$a-zA-Z\xA0-\uFFFF][$\w\xA0-\uFFFF]*(?=\s*\(|\.(?:apply|bind|call)\()/,
    'operator': /-[-=]?|\+[+=]?|!=?=?|<<?=?|>>?>?=?|=(?:==?|>)?|&[&=]?|\|[|=]?|\*\*?=?|\/=?|~|\^=?|%=?|\?|\.{3}/
});

Prism.languages.javascript['class-name'][0].pattern = /(\b(?:class|interface|extends|implements|instanceof|new)\s+)[\w.\\]+/

Prism.languages.insertBefore('javascript', 'keyword', {
    'regex': {
        pattern: /((?:^|[^$\w\xA0-\uFFFF."'\])\s])\s*)\/(\[(?:[^\]\\\r\n]|\\.)*]|\\.|[^/\\\[\r\n])+\/[gimyu]{0,5}(?=\s*($|[\r\n,.;})\]]))/,
        lookbehind: true,
        greedy: true
    },
    // This must be declared before keyword because we use "function" inside the look-forward
    'function-variable': {
        pattern: /[_$a-z\xA0-\uFFFF][$\w\xA0-\uFFFF]*(?=\s*[=:]\s*(?:async\s*)?(?:\bfunction\b|(?:\((?:[^()]|\([^()]*\))*\)|[_$a-z\xA0-\uFFFF][$\w\xA0-\uFFFF]*)\s*=>))/i,
        alias: 'function'
    },
    'parameter': [
        {
            pattern: /(function(?:\s+[_$A-Za-z\xA0-\uFFFF][$\w\xA0-\uFFFF]*)?\s*\(\s*)(?!\s)(?:[^()]|\([^()]*\))+?(?=\s*\))/,
            lookbehind: true,
            inside: Prism.languages.javascript
        },
        {
            pattern: /[_$a-z\xA0-\uFFFF][$\w\xA0-\uFFFF]*(?=\s*=>)/i,
            inside: Prism.languages.javascript
        },
        {
            pattern: /(\(\s*)(?!\s)(?:[^()]|\([^()]*\))+?(?=\s*\)\s*=>)/,
            lookbehind: true,
            inside: Prism.languages.javascript
        },
        {
            pattern: /((?:\b|\s|^)(?!(?:as|async|await|break|case|catch|class|const|continue|debugger|default|delete|do|else|enum|export|extends|finally|for|from|function|get|if|implements|import|in|instanceof|interface|let|new|null|of|package|private|protected|public|return|set|static|super|switch|this|throw|try|typeof|var|void|while|with|yield)(?![$\w\xA0-\uFFFF]))(?:[_$A-Za-z\xA0-\uFFFF][$\w\xA0-\uFFFF]*\s*)\(\s*)(?!\s)(?:[^()]|\([^()]*\))+?(?=\s*\)\s*\{)/,
            lookbehind: true,
            inside: Prism.languages.javascript
        }
    ],
    'constant': /\b[A-Z][A-Z\d_]*\b/
});

Prism.languages.insertBefore('javascript', 'string', {
    'template-string': {
        pattern: /`(?:\\[\s\S]|\${[^}]+}|[^\\`])*`/,
        greedy: true,
        inside: {
            'interpolation': {
                pattern: /\${[^}]+}/,
                inside: {
                    'interpolation-punctuation': {
                        pattern: /^\${|}$/,
                        alias: 'punctuation'
                    },
                    rest: Prism.languages.javascript
                }
            },
            'string': /[\s\S]+/
        }
    }
});

if (Prism.languages.markup) {
    Prism.languages.insertBefore('markup', 'tag', {
        'script': {
            pattern: /(<script[\s\S]*?>)[\s\S]*?(?=<\/script>)/i,
            lookbehind: true,
            inside: Prism.languages.javascript,
            alias: 'language-javascript',
            greedy: true
        }
    });
}

Prism.languages.js = Prism.languages.javascript;

Prism.languages.powershell = {
    'comment': [
        {
            pattern: /(^|[^`])<#[\s\S]*?#>/,
            lookbehind: true
        },
        {
            pattern: /(^|[^`])#.*/,
            lookbehind: true
        }
    ],
    'string': [
        {
            pattern: /"(?:`[\s\S]|[^`"])*"/,
            greedy: true,
            inside: {
                'function': {
                    // Allow for one level of nesting
                    pattern: /(^|[^`])\$\((?:\$\(.*?\)|(?!\$\()[^\r\n)])*\)/,
                    lookbehind: true,
                    // Populated at end of file
                    inside: {}
                }
            }
        },
        {
            pattern: /'(?:[^']|'')*'/,
            greedy: true
        }
    ],
    // Matches name spaces as well as casts, attribute decorators. Force starting with letter to avoid matching array indices
    // Supports two levels of nested brackets (e.g. `[OutputType([System.Collections.Generic.List[int]])]`)
    'namespace': /\[[a-z](?:\[(?:\[[^\]]*]|[^\[\]])*]|[^\[\]])*]/i,
    'boolean': /\$(?:true|false)\b/i,
    'variable': /\$\w+\b/i,
    // Cmdlets and aliases. Aliases should come last, otherwise "write" gets preferred over "write-host" for example
    // Get-Command | ?{ $_.ModuleName -match "Microsoft.PowerShell.(Util|Core|Management)" }
    // Get-Alias | ?{ $_.ReferencedCommand.Module.Name -match "Microsoft.PowerShell.(Util|Core|Management)" }
    'function': [
        /\b(?:Add-(?:Computer|Content|History|Member|PSSnapin|Type)|Checkpoint-Computer|Clear-(?:Content|EventLog|History|Item|ItemProperty|Variable)|Compare-Object|Complete-Transaction|Connect-PSSession|ConvertFrom-(?:Csv|Json|StringData)|Convert-Path|ConvertTo-(?:Csv|Html|Json|Xml)|Copy-(?:Item|ItemProperty)|Debug-Process|Disable-(?:ComputerRestore|PSBreakpoint|PSRemoting|PSSessionConfiguration)|Disconnect-PSSession|Enable-(?:ComputerRestore|PSBreakpoint|PSRemoting|PSSessionConfiguration)|Enter-PSSession|Exit-PSSession|Export-(?:Alias|Clixml|Console|Csv|FormatData|ModuleMember|PSSession)|ForEach-Object|Format-(?:Custom|List|Table|Wide)|Get-(?:Alias|ChildItem|Command|ComputerRestorePoint|Content|ControlPanelItem|Culture|Date|Event|EventLog|EventSubscriber|FormatData|Help|History|Host|HotFix|Item|ItemProperty|Job|Location|Member|Module|Process|PSBreakpoint|PSCallStack|PSDrive|PSProvider|PSSession|PSSessionConfiguration|PSSnapin|Random|Service|TraceSource|Transaction|TypeData|UICulture|Unique|Variable|WmiObject)|Group-Object|Import-(?:Alias|Clixml|Csv|LocalizedData|Module|PSSession)|Invoke-(?:Command|Expression|History|Item|RestMethod|WebRequest|WmiMethod)|Join-Path|Limit-EventLog|Measure-(?:Command|Object)|Move-(?:Item|ItemProperty)|New-(?:Alias|Event|EventLog|Item|ItemProperty|Module|ModuleManifest|Object|PSDrive|PSSession|PSSessionConfigurationFile|PSSessionOption|PSTransportOption|Service|TimeSpan|Variable|WebServiceProxy)|Out-(?:Default|File|GridView|Host|Null|Printer|String)|Pop-Location|Push-Location|Read-Host|Receive-(?:Job|PSSession)|Register-(?:EngineEvent|ObjectEvent|PSSessionConfiguration|WmiEvent)|Remove-(?:Computer|Event|EventLog|Item|ItemProperty|Job|Module|PSBreakpoint|PSDrive|PSSession|PSSnapin|TypeData|Variable|WmiObject)|Rename-(?:Computer|Item|ItemProperty)|Reset-ComputerMachinePassword|Resolve-Path|Restart-(?:Computer|Service)|Restore-Computer|Resume-(?:Job|Service)|Save-Help|Select-(?:Object|String|Xml)|Send-MailMessage|Set-(?:Alias|Content|Date|Item|ItemProperty|Location|PSBreakpoint|PSDebug|PSSessionConfiguration|Service|StrictMode|TraceSource|Variable|WmiInstance)|Show-(?:Command|ControlPanelItem|EventLog)|Sort-Object|Split-Path|Start-(?:Job|Process|Service|Sleep|Transaction)|Stop-(?:Computer|Job|Process|Service)|Suspend-(?:Job|Service)|Tee-Object|Test-(?:ComputerSecureChannel|Connection|ModuleManifest|Path|PSSessionConfigurationFile)|Trace-Command|Unblock-File|Undo-Transaction|Unregister-(?:Event|PSSessionConfiguration)|Update-(?:FormatData|Help|List|TypeData)|Use-Transaction|Wait-(?:Event|Job|Process)|Where-Object|Write-(?:Debug|Error|EventLog|Host|Output|Progress|Verbose|Warning))\b/i,
        /\b(?:ac|cat|chdir|clc|cli|clp|clv|compare|copy|cp|cpi|cpp|cvpa|dbp|del|diff|dir|ebp|echo|epal|epcsv|epsn|erase|fc|fl|ft|fw|gal|gbp|gc|gci|gcs|gdr|gi|gl|gm|gp|gps|group|gsv|gu|gv|gwmi|iex|ii|ipal|ipcsv|ipsn|irm|iwmi|iwr|kill|lp|ls|measure|mi|mount|move|mp|mv|nal|ndr|ni|nv|ogv|popd|ps|pushd|pwd|rbp|rd|rdr|ren|ri|rm|rmdir|rni|rnp|rp|rv|rvpa|rwmi|sal|saps|sasv|sbp|sc|select|set|shcm|si|sl|sleep|sls|sort|sp|spps|spsv|start|sv|swmi|tee|trcm|type|write)\b/i
    ],
    // per http://technet.microsoft.com/en-us/library/hh847744.aspx
    'keyword': /\b(?:Begin|Break|Catch|Class|Continue|Data|Define|Do|DynamicParam|Else|ElseIf|End|Exit|Filter|Finally|For|ForEach|From|Function|If|InlineScript|Parallel|Param|Process|Return|Sequence|Switch|Throw|Trap|Try|Until|Using|Var|While|Workflow)\b/i,
    'operator': {
        pattern: /(\W?)(?:!|-(eq|ne|gt|ge|lt|le|sh[lr]|not|b?(?:and|x?or)|(?:Not)?(?:Like|Match|Contains|In)|Replace|Join|is(?:Not)?|as)\b|-[-=]?|\+[+=]?|[*\/%]=?)/i,
        lookbehind: true
    },
    'punctuation': /[|{}[\];(),.]/
};

// Variable interpolation inside strings, and nested expressions
Prism.languages.powershell.string[0].inside.boolean = Prism.languages.powershell.boolean;
Prism.languages.powershell.string[0].inside.variable = Prism.languages.powershell.variable;
Prism.languages.powershell.string[0].inside.function.inside = Prism.languages.powershell;

(function (Prism) {
    Prism.languages.puppet = {
        'heredoc': [
            // Matches the content of a quoted heredoc string (subject to interpolation)
            {
                pattern: /(@\("([^"\r\n\/):]+)"(?:\/[nrts$uL]*)?\).*(?:\r?\n|\r))(?:.*(?:\r?\n|\r))*?[ \t]*\|?[ \t]*-?[ \t]*\2/,
                lookbehind: true,
                alias: 'string',
                inside: {
                    // Matches the end tag
                    'punctuation': /(?=\S).*\S(?= *$)/
                    // See interpolation below
                }
            },
            // Matches the content of an unquoted heredoc string (no interpolation)
            {
                pattern: /(@\(([^"\r\n\/):]+)(?:\/[nrts$uL]*)?\).*(?:\r?\n|\r))(?:.*(?:\r?\n|\r))*?[ \t]*\|?[ \t]*-?[ \t]*\2/,
                lookbehind: true,
                greedy: true,
                alias: 'string',
                inside: {
                    // Matches the end tag
                    'punctuation': /(?=\S).*\S(?= *$)/
                }
            },
            // Matches the start tag of heredoc strings
            {
                pattern: /@\("?(?:[^"\r\n\/):]+)"?(?:\/[nrts$uL]*)?\)/,
                alias: 'string',
                inside: {
                    'punctuation': {
                        pattern: /(\().+?(?=\))/,
                        lookbehind: true
                    }
                }
            }
        ],
        'multiline-comment': {
            pattern: /(^|[^\\])\/\*[\s\S]*?\*\//,
            lookbehind: true,
            greedy: true,
            alias: 'comment'
        },
        'regex': {
            // Must be prefixed with the keyword "node" or a non-word char
            pattern: /((?:\bnode\s+|[~=\(\[\{,]\s*|[=+]>\s*|^\s*))\/(?:[^\/\\]|\\[\s\S])+\/(?:[imx]+\b|\B)/,
            lookbehind: true,
            greedy: true,
            inside: {
                // Extended regexes must have the x flag. They can contain single-line comments.
                'extended-regex': {
                    pattern: /^\/(?:[^\/\\]|\\[\s\S])+\/[im]*x[im]*$/,
                    inside: {
                        'comment': /#.*/
                    }
                }
            }
        },
        'comment': {
            pattern: /(^|[^\\])#.*/,
            lookbehind: true,
            greedy: true,
        },
        'string': {
            // Allow for one nested level of double quotes inside interpolation
            pattern: /(["'])(?:\$\{(?:[^'"}]|(["'])(?:(?!\2)[^\\]|\\[\s\S])*\2)+\}|(?!\1)[^\\]|\\[\s\S])*\1/,
            greedy: true,
            inside: {
                'double-quoted': {
                    pattern: /^"[\s\S]*"$/,
                    inside: {
                        // See interpolation below
                    }
                }
            }
        },
        'variable': {
            pattern: /\$(?:::)?\w+(?:::\w+)*/,
            inside: {
                'punctuation': /::/
            }
        },
        'attr-name': /(?:\w+|\*)(?=\s*=>)/,
        'function': [
            {
                pattern: /(\.)(?!\d)\w+/,
                lookbehind: true
            },
            /\b(?:contain|debug|err|fail|include|info|notice|realize|require|tag|warning)\b|\b(?!\d)\w+(?=\()/
        ],
        'number': /\b(?:0x[a-f\d]+|\d+(?:\.\d+)?(?:e-?\d+)?)\b/i,
        'boolean': /\b(?:true|false)\b/,
        // Includes words reserved for future use
        'keyword': /\b(?:application|attr|case|class|consumes|default|define|else|elsif|function|if|import|inherits|node|private|produces|type|undef|unless)\b/,
        'datatype': {
            pattern: /\b(?:Any|Array|Boolean|Callable|Catalogentry|Class|Collection|Data|Default|Enum|Float|Hash|Integer|NotUndef|Numeric|Optional|Pattern|Regexp|Resource|Runtime|Scalar|String|Struct|Tuple|Type|Undef|Variant)\b/,
            alias: 'symbol'
        },
        'operator': /=[=~>]?|![=~]?|<(?:<\|?|[=~|-])?|>[>=]?|->?|~>|\|>?>?|[*\/%+?]|\b(?:and|in|or)\b/,
        'punctuation': /[\[\]{}().,;]|:+/
    };

    var interpolation = [
        {
            // Allow for one nested level of braces inside interpolation
            pattern: /(^|[^\\])\$\{(?:[^'"{}]|\{[^}]*\}|(["'])(?:(?!\2)[^\\]|\\[\s\S])*\2)+\}/,
            lookbehind: true,
            inside: {
                'short-variable': {
                    // Negative look-ahead prevent wrong highlighting of functions
                    pattern: /(^\$\{)(?!\w+\()(?:::)?\w+(?:::\w+)*/,
                    lookbehind: true,
                    alias: 'variable',
                    inside: {
                        'punctuation': /::/
                    }
                },
                'delimiter': {
                    pattern: /^\$/,
                    alias: 'variable'
                },
                rest: Prism.languages.puppet
            }
        },
        {
            pattern: /(^|[^\\])\$(?:::)?\w+(?:::\w+)*/,
            lookbehind: true,
            alias: 'variable',
            inside: {
                'punctuation': /::/
            }
        }
    ];
    Prism.languages.puppet['heredoc'][0].inside.interpolation = interpolation;
    Prism.languages.puppet['string'].inside['double-quoted'].inside.interpolation = interpolation;
}(Prism));

(function (Prism) {
    Prism.languages.ruby = Prism.languages.extend('clike', {
        'comment': [
            /#.*/,
            {
                pattern: /^=begin\s[\s\S]*?^=end/m,
                greedy: true
            }
        ],
        'keyword': /\b(?:alias|and|BEGIN|begin|break|case|class|def|define_method|defined|do|each|else|elsif|END|end|ensure|false|for|if|in|module|new|next|nil|not|or|protected|private|public|raise|redo|require|rescue|retry|return|self|super|then|throw|true|undef|unless|until|when|while|yield)\b/
    });

    var interpolation = {
        pattern: /#\{[^}]+\}/,
        inside: {
            'delimiter': {
                pattern: /^#\{|\}$/,
                alias: 'tag'
            },
            rest: Prism.languages.ruby
        }
    };

    delete Prism.languages.ruby.function;

    Prism.languages.insertBefore('ruby', 'keyword', {
        'regex': [
            {
                pattern: /%r([^a-zA-Z0-9\s{(\[<])(?:(?!\1)[^\\]|\\[\s\S])*\1[gim]{0,3}/,
                greedy: true,
                inside: {
                    'interpolation': interpolation
                }
            },
            {
                pattern: /%r\((?:[^()\\]|\\[\s\S])*\)[gim]{0,3}/,
                greedy: true,
                inside: {
                    'interpolation': interpolation
                }
            },
            {
                // Here we need to specifically allow interpolation
                pattern: /%r\{(?:[^#{}\\]|#(?:\{[^}]+\})?|\\[\s\S])*\}[gim]{0,3}/,
                greedy: true,
                inside: {
                    'interpolation': interpolation
                }
            },
            {
                pattern: /%r\[(?:[^\[\]\\]|\\[\s\S])*\][gim]{0,3}/,
                greedy: true,
                inside: {
                    'interpolation': interpolation
                }
            },
            {
                pattern: /%r<(?:[^<>\\]|\\[\s\S])*>[gim]{0,3}/,
                greedy: true,
                inside: {
                    'interpolation': interpolation
                }
            },
            {
                pattern: /(^|[^/])\/(?!\/)(\[.+?]|\\.|[^/\\\r\n])+\/[gim]{0,3}(?=\s*($|[\r\n,.;})]))/,
                lookbehind: true,
                greedy: true
            }
        ],
        'variable': /[@$]+[a-zA-Z_]\w*(?:[?!]|\b)/,
        'symbol': {
            pattern: /(^|[^:]):[a-zA-Z_]\w*(?:[?!]|\b)/,
            lookbehind: true
        },
        'method-definition': {
            pattern: /(\bdef\s+)[\w.]+/,
            lookbehind: true,
            inside: {
                'function': /\w+$/,
                rest: Prism.languages.ruby
            }
        }
    });

    Prism.languages.insertBefore('ruby', 'number', {
        'builtin': /\b(?:Array|Bignum|Binding|Class|Continuation|Dir|Exception|FalseClass|File|Stat|Fixnum|Float|Hash|Integer|IO|MatchData|Method|Module|NilClass|Numeric|Object|Proc|Range|Regexp|String|Struct|TMS|Symbol|ThreadGroup|Thread|Time|TrueClass)\b/,
        'constant': /\b[A-Z]\w*(?:[?!]|\b)/
    });

    Prism.languages.ruby.string = [
        {
            pattern: /%[qQiIwWxs]?([^a-zA-Z0-9\s{(\[<])(?:(?!\1)[^\\]|\\[\s\S])*\1/,
            greedy: true,
            inside: {
                'interpolation': interpolation
            }
        },
        {
            pattern: /%[qQiIwWxs]?\((?:[^()\\]|\\[\s\S])*\)/,
            greedy: true,
            inside: {
                'interpolation': interpolation
            }
        },
        {
            // Here we need to specifically allow interpolation
            pattern: /%[qQiIwWxs]?\{(?:[^#{}\\]|#(?:\{[^}]+\})?|\\[\s\S])*\}/,
            greedy: true,
            inside: {
                'interpolation': interpolation
            }
        },
        {
            pattern: /%[qQiIwWxs]?\[(?:[^\[\]\\]|\\[\s\S])*\]/,
            greedy: true,
            inside: {
                'interpolation': interpolation
            }
        },
        {
            pattern: /%[qQiIwWxs]?<(?:[^<>\\]|\\[\s\S])*>/,
            greedy: true,
            inside: {
                'interpolation': interpolation
            }
        },
        {
            pattern: /("|')(?:#\{[^}]+\}|\\(?:\r\n|[\s\S])|(?!\1)[^\\\r\n])*\1/,
            greedy: true,
            inside: {
                'interpolation': interpolation
            }
        }
    ];

    Prism.languages.rb = Prism.languages.ruby;
}(Prism));

Prism.languages.python = {
    'comment': {
        pattern: /(^|[^\\])#.*/,
        lookbehind: true
    },
    'string-interpolation': {
        pattern: /(?:f|rf|fr)(?:("""|''')[\s\S]+?\1|("|')(?:\\.|(?!\2)[^\\\r\n])*\2)/i,
        greedy: true,
        inside: {
            'interpolation': {
                // "{" <expression> <optional "!s", "!r", or "!a"> <optional ":" format specifier> "}"
                pattern: /((?:^|[^{])(?:{{)*){(?!{)(?:[^{}]|{(?!{)(?:[^{}]|{(?!{)(?:[^{}])+})+})+}/,
                lookbehind: true,
                inside: {
                    'format-spec': {
                        pattern: /(:)[^:(){}]+(?=}$)/,
                        lookbehind: true
                    },
                    'conversion-option': {
                        pattern: /![sra](?=[:}]$)/,
                        alias: 'punctuation'
                    },
                    rest: null
                }
            },
            'string': /[\s\S]+/
        }
    },
    'triple-quoted-string': {
        pattern: /(?:[rub]|rb|br)?("""|''')[\s\S]+?\1/i,
        greedy: true,
        alias: 'string'
    },
    'string': {
        pattern: /(?:[rub]|rb|br)?("|')(?:\\.|(?!\1)[^\\\r\n])*\1/i,
        greedy: true
    },
    'function': {
        pattern: /((?:^|\s)def[ \t]+)[a-zA-Z_]\w*(?=\s*\()/g,
        lookbehind: true
    },
    'class-name': {
        pattern: /(\bclass\s+)\w+/i,
        lookbehind: true
    },
    'decorator': {
        pattern: /(^\s*)@\w+(?:\.\w+)*/i,
        lookbehind: true,
        alias: ['annotation', 'punctuation'],
        inside: {
            'punctuation': /\./
        }
    },
    'keyword': /\b(?:and|as|assert|async|await|break|class|continue|def|del|elif|else|except|exec|finally|for|from|global|if|import|in|is|lambda|nonlocal|not|or|pass|print|raise|return|try|while|with|yield)\b/,
    'builtin': /\b(?:__import__|abs|all|any|apply|ascii|basestring|bin|bool|buffer|bytearray|bytes|callable|chr|classmethod|cmp|coerce|compile|complex|delattr|dict|dir|divmod|enumerate|eval|execfile|file|filter|float|format|frozenset|getattr|globals|hasattr|hash|help|hex|id|input|int|intern|isinstance|issubclass|iter|len|list|locals|long|map|max|memoryview|min|next|object|oct|open|ord|pow|property|range|raw_input|reduce|reload|repr|reversed|round|set|setattr|slice|sorted|staticmethod|str|sum|super|tuple|type|unichr|unicode|vars|xrange|zip)\b/,
    'boolean': /\b(?:True|False|None)\b/,
    'number': /(?:\b(?=\d)|\B(?=\.))(?:0[bo])?(?:(?:\d|0x[\da-f])[\da-f]*\.?\d*|\.\d+)(?:e[+-]?\d+)?j?\b/i,
    'operator': /[-+%=]=?|!=|\*\*?=?|\/\/?=?|<[<=>]?|>[=>]?|[&|^~]/,
    'punctuation': /[{}[\];(),.:]/
};

Prism.languages.python['string-interpolation'].inside['interpolation'].inside.rest = Prism.languages.python;

Prism.languages.py = Prism.languages.python;
Prism.languages.yaml = {
    'scalar': {
        pattern: /([\-:]\s*(?:![^\s]+)?[ \t]*[|>])[ \t]*(?:((?:\r?\n|\r)[ \t]+)[^\r\n]+(?:\2[^\r\n]+)*)/,
        lookbehind: true,
        alias: 'string'
    },
    'comment': /#.*/,
    'key': {
        pattern: /(\s*(?:^|[:\-,[{\r\n?])[ \t]*(?:![^\s]+)?[ \t]*)[^\r\n{[\]},#\s]+?(?=\s*:\s)/,
        lookbehind: true,
        alias: 'atrule'
    },
    'directive': {
        pattern: /(^[ \t]*)%.+/m,
        lookbehind: true,
        alias: 'important'
    },
    'datetime': {
        pattern: /([:\-,[{]\s*(?:![^\s]+)?[ \t]*)(?:\d{4}-\d\d?-\d\d?(?:[tT]|[ \t]+)\d\d?:\d{2}:\d{2}(?:\.\d*)?[ \t]*(?:Z|[-+]\d\d?(?::\d{2})?)?|\d{4}-\d{2}-\d{2}|\d\d?:\d{2}(?::\d{2}(?:\.\d*)?)?)(?=[ \t]*(?:$|,|]|}))/m,
        lookbehind: true,
        alias: 'number'
    },
    'boolean': {
        pattern: /([:\-,[{]\s*(?:![^\s]+)?[ \t]*)(?:true|false)[ \t]*(?=$|,|]|})/im,
        lookbehind: true,
        alias: 'important'
    },
    'null': {
        pattern: /([:\-,[{]\s*(?:![^\s]+)?[ \t]*)(?:null|~)[ \t]*(?=$|,|]|})/im,
        lookbehind: true,
        alias: 'important'
    },
    'string': {
        pattern: /([:\-,[{]\s*(?:![^\s]+)?[ \t]*)("|')(?:(?!\2)[^\\\r\n]|\\.)*\2(?=[ \t]*(?:$|,|]|}|\s*#))/m,
        lookbehind: true,
        greedy: true
    },
    'number': {
        pattern: /([:\-,[{]\s*(?:![^\s]+)?[ \t]*)[+-]?(?:0x[\da-f]+|0o[0-7]+|(?:\d+\.?\d*|\.?\d+)(?:e[+-]?\d+)?|\.inf|\.nan)[ \t]*(?=$|,|]|})/im,
        lookbehind: true
    },
    'tag': /![^\s]+/,
    'important': /[&*][\w]+/,
    'punctuation': /---|[:[\]{}\-,|>?]|\.\.\./
};

Prism.languages.yml = Prism.languages.yaml;

(function () {

    if (typeof self === 'undefined' || !self.Prism || !self.document) {
        return;
    }

	/**
	 * Plugin name which is used as a class name for <pre> which is activating the plugin
	 * @type {String}
	 */
    var PLUGIN_NAME = 'line-numbers';

	/**
	 * Regular expression used for determining line breaks
	 * @type {RegExp}
	 */
    var NEW_LINE_EXP = /\n(?!$)/g;

	/**
	 * Resizes line numbers spans according to height of line of code
	 * @param {Element} element <pre> element
	 */
    var _resizeElement = function (element) {
        var codeStyles = getStyles(element);
        var whiteSpace = codeStyles['white-space'];

        if (whiteSpace === 'pre-wrap' || whiteSpace === 'pre-line') {
            var codeElement = element.querySelector('code');
            var lineNumbersWrapper = element.querySelector('.line-numbers-rows');
            var lineNumberSizer = element.querySelector('.line-numbers-sizer');
            var codeLines = codeElement.textContent.split(NEW_LINE_EXP);

            if (!lineNumberSizer) {
                lineNumberSizer = document.createElement('span');
                lineNumberSizer.className = 'line-numbers-sizer';

                codeElement.appendChild(lineNumberSizer);
            }

            lineNumberSizer.style.display = 'block';

            codeLines.forEach(function (line, lineNumber) {
                lineNumberSizer.textContent = line || '\n';
                var lineSize = lineNumberSizer.getBoundingClientRect().height;
                lineNumbersWrapper.children[lineNumber].style.height = lineSize + 'px';
            });

            lineNumberSizer.textContent = '';
            lineNumberSizer.style.display = 'none';
        }
    };

	/**
	 * Returns style declarations for the element
	 * @param {Element} element
	 */
    var getStyles = function (element) {
        if (!element) {
            return null;
        }

        return window.getComputedStyle ? getComputedStyle(element) : (element.currentStyle || null);
    };

    window.addEventListener('resize', function () {
        Array.prototype.forEach.call(document.querySelectorAll('pre.' + PLUGIN_NAME), _resizeElement);
    });

    Prism.hooks.add('complete', function (env) {
        if (!env.code) {
            return;
        }

        // works only for <code> wrapped inside <pre> (not inline)
        var pre = env.element.parentNode;
        var clsReg = /\s*\bline-numbers\b\s*/;
        if (
            !pre || !/pre/i.test(pre.nodeName) ||
            // Abort only if nor the <pre> nor the <code> have the class
            (!clsReg.test(pre.className) && !clsReg.test(env.element.className))
        ) {
            return;
        }

        if (env.element.querySelector('.line-numbers-rows')) {
            // Abort if line numbers already exists
            return;
        }

        if (clsReg.test(env.element.className)) {
            // Remove the class 'line-numbers' from the <code>
            env.element.className = env.element.className.replace(clsReg, ' ');
        }
        if (!clsReg.test(pre.className)) {
            // Add the class 'line-numbers' to the <pre>
            pre.className += ' line-numbers';
        }

        var match = env.code.match(NEW_LINE_EXP);
        var linesNum = match ? match.length + 1 : 1;
        var lineNumbersWrapper;

        var lines = new Array(linesNum + 1);
        lines = lines.join('<span></span>');

        lineNumbersWrapper = document.createElement('span');
        lineNumbersWrapper.setAttribute('aria-hidden', 'true');
        lineNumbersWrapper.className = 'line-numbers-rows';
        lineNumbersWrapper.innerHTML = lines;

        if (pre.hasAttribute('data-start')) {
            pre.style.counterReset = 'linenumber ' + (parseInt(pre.getAttribute('data-start'), 10) - 1);
        }

        env.element.appendChild(lineNumbersWrapper);

        _resizeElement(pre);

        Prism.hooks.run('line-numbers', env);
    });

    Prism.hooks.add('line-numbers', function (env) {
        env.plugins = env.plugins || {};
        env.plugins.lineNumbers = true;
    });

	/**
	 * Global exports
	 */
    Prism.plugins.lineNumbers = {
		/**
		 * Get node for provided line number
		 * @param {Element} element pre element
		 * @param {Number} number line number
		 * @return {Element|undefined}
		 */
        getLine: function (element, number) {
            if (element.tagName !== 'PRE' || !element.classList.contains(PLUGIN_NAME)) {
                return;
            }

            var lineNumberRows = element.querySelector('.line-numbers-rows');
            var lineNumberStart = parseInt(element.getAttribute('data-start'), 10) || 1;
            var lineNumberEnd = lineNumberStart + (lineNumberRows.children.length - 1);

            if (number < lineNumberStart) {
                number = lineNumberStart;
            }
            if (number > lineNumberEnd) {
                number = lineNumberEnd;
            }

            var lineIndex = number - lineNumberStart;

            return lineNumberRows.children[lineIndex];
        }
    };

}());
(function () {
    if (typeof self === 'undefined' || !self.Prism || !self.document) {
        return;
    }

    var callbacks = [];
    var map = {};
    var noop = function () { };

    Prism.plugins.toolbar = {};

	/**
	 * Register a button callback with the toolbar.
	 *
	 * @param {string} key
	 * @param {Object|Function} opts
	 */
    var registerButton = Prism.plugins.toolbar.registerButton = function (key, opts) {
        var callback;

        if (typeof opts === 'function') {
            callback = opts;
        } else {
            callback = function (env) {
                var element;

                if (typeof opts.onClick === 'function') {
                    element = document.createElement('button');
                    element.type = 'button';
                    element.addEventListener('click', function () {
                        opts.onClick.call(this, env);
                    });
                } else if (typeof opts.url === 'string') {
                    element = document.createElement('a');
                    element.href = opts.url;
                } else {
                    element = document.createElement('span');
                }

                element.textContent = opts.text;

                return element;
            };
        }

        callbacks.push(map[key] = callback);
    };

	/**
	 * Post-highlight Prism hook callback.
	 *
	 * @param env
	 */
    var hook = Prism.plugins.toolbar.hook = function (env) {
        // Check if inline or actual code block (credit to line-numbers plugin)
        var pre = env.element.parentNode;
        if (!pre || !/pre/i.test(pre.nodeName)) {
            return;
        }

        // Autoloader rehighlights, so only do this once.
        if (pre.parentNode.classList.contains('code-toolbar')) {
            return;
        }

        // Create wrapper for <pre> to prevent scrolling toolbar with content
        var wrapper = document.createElement("div");
        wrapper.classList.add("code-toolbar");
        pre.parentNode.insertBefore(wrapper, pre);
        wrapper.appendChild(pre);

        // Setup the toolbar
        var toolbar = document.createElement('div');
        toolbar.classList.add('toolbar');

        if (document.body.hasAttribute('data-toolbar-order')) {
            callbacks = document.body.getAttribute('data-toolbar-order').split(',').map(function (key) {
                return map[key] || noop;
            });
        }

        callbacks.forEach(function (callback) {
            var element = callback(env);

            if (!element) {
                return;
            }

            var item = document.createElement('div');
            item.classList.add('toolbar-item');

            item.appendChild(element);
            toolbar.appendChild(item);
        });

        // Add our toolbar to the currently created wrapper of <pre> tag
        wrapper.appendChild(toolbar);
    };

    registerButton('label', function (env) {
        var pre = env.element.parentNode;
        if (!pre || !/pre/i.test(pre.nodeName)) {
            return;
        }

        if (!pre.hasAttribute('data-label')) {
            return;
        }

        var element, template;
        var text = pre.getAttribute('data-label');
        try {
            // Any normal text will blow up this selector.
            template = document.querySelector('template#' + text);
        } catch (e) { }

        if (template) {
            element = template.content;
        } else {
            if (pre.hasAttribute('data-url')) {
                element = document.createElement('a');
                element.href = pre.getAttribute('data-url');
            } else {
                element = document.createElement('span');
            }

            element.textContent = text;
        }

        return element;
    });

	/**
	 * Register the toolbar with Prism.
	 */
    Prism.hooks.add('complete', hook);
})();

(function () {

    if (typeof self === 'undefined' || !self.Prism || !self.document || !Prism.languages.markup) {
        return;
    }

    Prism.plugins.UnescapedMarkup = true;

    Prism.hooks.add('before-highlightall', function (env) {
        env.selector += ", [class*='lang-'] script[type='text/plain'], [class*='language-'] script[type='text/plain']" +
            ", script[type='text/plain'][class*='lang-'], script[type='text/plain'][class*='language-']";
    });

    Prism.hooks.add('before-sanity-check', function (env) {
        if ((env.element.matches || env.element.msMatchesSelector).call(env.element, "script[type='text/plain']")) {
            var code = document.createElement("code");
            var pre = document.createElement("pre");

            pre.className = code.className = env.element.className;

            if (env.element.dataset) {
                Object.keys(env.element.dataset).forEach(function (key) {
                    if (Object.prototype.hasOwnProperty.call(env.element.dataset, key)) {
                        pre.dataset[key] = env.element.dataset[key];
                    }
                });
            }

            env.code = env.code.replace(/&lt;\/script(>|&gt;)/gi, "</scri" + "pt>");
            code.textContent = env.code;

            pre.appendChild(code);
            env.element.parentNode.replaceChild(pre, env.element);
            env.element = code;
            return;
        }

        var pre = env.element.parentNode;
        if (!env.code && pre && pre.nodeName.toLowerCase() == 'pre' &&
            env.element.childNodes.length && env.element.childNodes[0].nodeName == "#comment") {
            env.element.textContent = env.code = env.element.childNodes[0].textContent;
        }
    });
}());

(function () {
    if (typeof self === 'undefined' || !self.Prism || !self.document) {
        return;
    }

    if (!Prism.plugins.toolbar) {
        console.warn('Copy to Clipboard plugin loaded before Toolbar plugin.');

        return;
    }

    var ClipboardJS = window.ClipboardJS || undefined;

    if (!ClipboardJS && typeof require === 'function') {
        ClipboardJS = require('clipboard');
    }

    var callbacks = [];

    if (!ClipboardJS) {
        var script = document.createElement('script');
        var head = document.querySelector('head');

        script.onload = function () {
            ClipboardJS = window.ClipboardJS;

            if (ClipboardJS) {
                while (callbacks.length) {
                    callbacks.pop()();
                }
            }
        };

        script.src = 'https://cdnjs.cloudflare.com/ajax/libs/clipboard.js/2.0.0/clipboard.min.js';
        head.appendChild(script);
    }

    Prism.plugins.toolbar.registerButton('copy-to-clipboard', function (env) {
        var linkCopy = document.createElement('a');
        linkCopy.textContent = 'Copy';

        if (!ClipboardJS) {
            callbacks.push(registerClipboard);
        } else {
            registerClipboard();
        }

        return linkCopy;

        function registerClipboard() {
            var clip = new ClipboardJS(linkCopy, {
                'text': function () {
                    return env.code;
                }
            });

            clip.on('success', function () {
                linkCopy.textContent = 'Copied!';

                resetText();
            });
            clip.on('error', function () {
                linkCopy.textContent = 'Press Ctrl+C to copy';

                resetText();
            });
        }

        function resetText() {
            setTimeout(function () {
                linkCopy.textContent = 'Copy';
            }, 5000);
        }
    });
})();