import clsx from 'clsx';
import Link from '@docusaurus/Link';
import useDocusaurusContext from '@docusaurus/useDocusaurusContext';
import type { JSX, ReactNode } from 'react';

import Heading from '@theme/Heading';
import Layout from '@theme/Layout';
import HomepageFeatures from '@site/src/components/HomepageFeatures';

import styles from './index.module.css';

/**
 * The header section on the home page
 * @returns A React component representing the header section of the home page
 */
function HomepageHeader(): JSX.Element {
  const { siteConfig } = useDocusaurusContext();
  return (
    <header className={clsx('hero hero--primary', styles.heroBanner)}>
      <div className="container">
        <Heading as="h1" className="hero__title">
          {siteConfig.title}
        </Heading>
        <p className="hero__subtitle">{siteConfig.tagline}</p>
        <p className={styles.epigraph}>
          <em>
            "Things are seldom what they seem; skim milk masquerades as cream."
          </em>
          <br />
          <small>— H.M.S. Pinafore</small>
        </p>
        <div className={styles.buttons}>
          <Link
            className="button button--secondary button--lg"
            to="/docs/guide"
          >
            Language Guide
          </Link>
          <Link
            className="button button--outline button--secondary button--lg"
            to="/docs/tutorials"
          >
            Tutorials
          </Link>
        </div>
      </div>
    </header>
  );
}

/**
 * The main component for the home page.
 * @returns A React component for the home page.
 */
export default function Home(): ReactNode {
  const { siteConfig } = useDocusaurusContext();
  return (
    <Layout
      title={siteConfig.title}
      description="Documentation for Topsy Turvy, an Esoteric Programming Language themed around Gilbert & Sullivan!">
      <HomepageHeader />
      <main>
        <HomepageFeatures />
        <section className={styles.snippet}>
          <div className="container">
            <Heading as="h2" className="text--center">
              A Sample Programme
            </Heading>
            <pre className={styles.codeBlock}>
              <code>{`HARK! "An Arithmetical Operetta"
  or, "How to Add the Lords"

PRINCIPALS:
  PRAY WELCOME Greeting AS A YARN
  PRAY WELCOME Lords AS A PEER BEING 0
CURTAIN RISES.

Greeting IS APPOINTED "Good Morrow, World!  World, Good Morrow!"
BEHOLD Greeting

PRAY WELCOME ConservativePeers AS A PEER BEING 7
PRAY WELCOME LiberalPeers AS A PEER BEING 6

Lords IS APPOINTED SUM OF ConservativePeers AND LiberalPeers
BEHOLD "Total Peers: {Lords}."

FINALE.`}</code>
            </pre>
          </div>
        </section>
      </main>
    </Layout>
  );
}
