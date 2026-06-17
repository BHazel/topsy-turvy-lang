import clsx from 'clsx';
import type { ReactNode } from 'react';

import Heading from '@theme/Heading';

import styles from './styles.module.css';

/**
 * Represents a feature.
 */
type FeatureItem = {
  /**
   * The title of the feature.
   */
  title: string;

  /**
   * Theicon emoji representing the feature.
   */
  icon: string;

  /**
   * The description of the feature.
   */
  description: ReactNode;
};

/**
 * The list of features to display on the homepage.
 */
const FeatureList: FeatureItem[] = [
  {
    title: 'G&S Inspired Syntax',
    icon: '🎭',
    description: (
      <>
        Write programmes inspired by the libretti of Gilbert & Sullivan!  <em>Welcome</em>
        your variables, <em>behold</em> output, fulfill your <em>duty</em> with functions
        and throw a <em>hideous curse</em> on errors!
      </>
    ),
  },
  {
    title: 'Full Toolchain',
    icon: '⚙️',
    description: (
      <>
        Create, check and run programes in your environment of choice: the
        <code>operetta</code> CLI, a web editor or a fully-featured VS Code
        extension!  All powered by the power of .NET.
      </>
    ),
  },
  {
    title: 'Detailed Documentation',
    icon: '📜',
    description: (
      <>
        Read extensive tutorials for all language features and a detailed reference
        of how the toolchain works, intended to be accessible to beginners and
        experts alike, including a full API reference.
      </>
    ),
  },
];

/**
 * A home page feature.
 * @param param0 The feature item to display.
 * @returns A React node representing the feature.
 */
function Feature({title, icon, description}: FeatureItem) {
  return (
    <div className={clsx('col col--4')}>
      <div className="text--center">
        <span className={styles.featureIcon} role="img" aria-label={title}>
          {icon}
        </span>
      </div>
      <div className="text--center padding-horiz--md">
        <Heading as="h3">{title}</Heading>
        <p>{description}</p>
      </div>
    </div>
  );
}

/**
 * The home page features.
 * @returns A React node representing the home page features.
 */
export default function HomepageFeatures(): ReactNode {
  return (
    <section className={styles.features}>
      <div className="container">
        <div className="row">
          {FeatureList.map((props, idx) => (
            <Feature key={idx} {...props} />
          ))}
        </div>
      </div>
    </section>
  );
}
