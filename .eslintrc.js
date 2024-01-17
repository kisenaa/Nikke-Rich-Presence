module.exports = {
    env: {
      browser: true,
      node: true,
    },
    ignorePatterns: [
      'node_modules/',
      'node_modules/*',
      'node_modules/**',
      './node_modules/**',
    ],
    settings: {
      react: {
        version: 'detect',
      },
    },
    overrides: [
      {
        files: ['*.ts', '*.tsx', '*.mts'],
        parser: '@typescript-eslint/parser',
        parserOptions: {
          ecmaVersion: 'latest',
          ecmaFeatures: {
            jsx: true,
          }
        },
        plugins: [
          '@typescript-eslint',
          'simple-import-sort',
          'unused-imports',
          'react',
          'tailwindcss',
        ],
        extends: [
          'eslint:recommended',
          'plugin:@typescript-eslint/recommended',
          'plugin:tailwindcss/recommended',
          'plugin:react/recommended',
          'plugin:react/jsx-runtime',
          'prettier',
        ],
        rules: {
          'no-unused-vars': 'off',
          'no-console': 'off',
          '@typescript-eslint/ban-ts-comment': 'off',
          '@typescript-eslint/explicit-module-boundary-types': 'off',
      
          'react/display-name': 'off',
          'react/jsx-curly-brace-presence': [
            'warn',
            { props: 'never', children: 'never' },
          ],
          "react/no-unknown-property": 'warn',
      
          //#region  //*=========== Unused Import ===========
          '@typescript-eslint/no-unused-vars': 'off',
          'unused-imports/no-unused-imports': 'warn',
          'unused-imports/no-unused-vars': [
            'warn',
            {
              vars: 'all',
              varsIgnorePattern: '^_',
              args: 'after-used',
              argsIgnorePattern: '^_',
            },
          ],
          //#endregion  //*======== Unused Import ===========
      
          // Sort
          'simple-import-sort/exports': 'warn',
          'simple-import-sort/imports': [
            'warn',
            {
              groups: [
                // ext library & side effect imports
                ['^@?\\w', '^\\u0000'],
                // {s}css files
                ['^.+\\.s?css$'],
                // Lib and hooks
                ['^@/lib', '^@/hooks'],
                // static data
                ['^@/constant'],
                // components
                ['^@/components'],
                // assets
                ['^@/assets'],
                // Other imports
                ['^@/'],
                // relative paths up until 3 level
                [
                  '^\\./?$',
                  '^\\.(?!/?$)',
                  '^\\.\\./?$',
                  '^\\.\\.(?!/?$)',
                  '^\\.\\./\\.\\./?$',
                  '^\\.\\./\\.\\.(?!/?$)',
                  '^\\.\\./\\.\\./\\.\\./?$',
                  '^\\.\\./\\.\\./\\.\\.(?!/?$)',
                ],
                ['^@/types'],
                // other that didnt fit in
                ['^'],
              ],
            },
          ],
        },
      },
    ],
    globals: {
      React: true,
      JSX: true,
    },
  };